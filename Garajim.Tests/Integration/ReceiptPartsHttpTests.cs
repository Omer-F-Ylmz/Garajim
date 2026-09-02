using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Business.Abstract;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class ReceiptPartsHttpTests : IDisposable
    {
        private sealed class SahteExtractor : IReceiptExtractor
        {
            public ReceiptExtractionResult Sonuc { get; set; } = new ReceiptExtractionResult();

            public Task<ReceiptExtractionResult> ExtractAsync(byte[] imageBytes, string mimeType, CancellationToken ct)
            {
                return Task.FromResult(Sonuc);
            }
        }

        private sealed class ParcaFactory : GarajimWebApplicationFactory
        {
            private readonly string _klasor;

            public SahteExtractor Extractor { get; } = new SahteExtractor();

            public ParcaFactory(string klasor)
            {
                _klasor = klasor;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Documents:StoragePath"] = _klasor
                }));
                builder.ConfigureServices(services =>
                {
                    foreach (var kayit in services.Where(s => s.ServiceType == typeof(IReceiptExtractor)).ToList())
                    {
                        services.Remove(kayit);
                    }
                    services.AddSingleton<IReceiptExtractor>(Extractor);
                });
            }
        }

        private static readonly byte[] PngIcerik = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };

        private readonly string _klasor;
        private readonly ParcaFactory _factory;

        public ReceiptPartsHttpTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-fisparca-" + Guid.NewGuid().ToString("N"));
            _factory = new ParcaFactory(_klasor);
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "Fiş Parça", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 120000,
                fuelType = "Benzin"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        private static Task<HttpResponseMessage> YukleAsync(HttpClient client)
        {
            var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(PngIcerik);
            dosya.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(dosya, "file", "servis.png");
            return client.PostAsync("/api/Receipts?otoOnay=false", form);
        }

        [Fact]
        public async Task TaslakKalemlerdenParcaListesiCikarir()
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "34FPR001");

            _factory.Extractor.Sonuc = new ReceiptExtractionResult
            {
                Tarih = new DateTime(2026, 8, 10),
                ToplamTutar = 2650m,
                TahminiTur = ReceiptType.Bakim,
                GuvenSkoru = 0.8,
                KalemListesi = new List<ReceiptItemResult>
                {
                    new ReceiptItemResult { Ad = "MOTOR YAĞI 5W30", Tutar = 1800m },
                    new ReceiptItemResult { Ad = "YAĞ FİLTRESİ", Tutar = 350m },
                    new ReceiptItemResult { Ad = "İŞÇİLİK", Tutar = 500m }
                }
            };

            var zarf = await VeriAsync(await YukleAsync(sahip));
            var parcalar = zarf.GetProperty("taslak").GetProperty("parcalar");

            Assert.Equal(2, parcalar.GetArrayLength());
            Assert.Equal("MotorYagi", parcalar[0].GetProperty("parcaTuru").GetString());
            Assert.Equal("YagFiltresi", parcalar[1].GetProperty("parcaTuru").GetString());
        }

        [Fact]
        public async Task OnayBakimdaMaintenancePartYaratir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34FPR002");

            _factory.Extractor.Sonuc = new ReceiptExtractionResult
            {
                Tarih = new DateTime(2026, 8, 10),
                ToplamTutar = 2150m,
                TahminiTur = ReceiptType.Bakim,
                GuvenSkoru = 0.8,
                KalemListesi = new List<ReceiptItemResult>
                {
                    new ReceiptItemResult { Ad = "MOTOR YAĞI 5W30", Tutar = 1800m },
                    new ReceiptItemResult { Ad = "YAĞ FİLTRESİ", Tutar = 350m }
                }
            };

            var taslakId = (await VeriAsync(await YukleAsync(sahip))).GetProperty("taslakId").GetInt32();

            await sahip.PostAsJsonAsync($"/api/Receipts/{taslakId}/confirm", new
            {
                vehicleId = aracId,
                tur = "Bakim",
                tarih = "2026-08-10",
                tutar = 2150m,
                km = 121000,
                bakimTuru = "YagDegisimi"
            });

            var hafiza = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/parca-hafizasi"));
            var turler = hafiza.EnumerateArray().Select(p => p.GetProperty("parcaTuru").GetString()).ToList();

            Assert.Contains("MotorYagi", turler);
            Assert.Contains("YagFiltresi", turler);
        }

        [Fact]
        public async Task ParcaDegisikligiTekBayrakOlarakIsaretlenir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34FPR003");

            _factory.Extractor.Sonuc = new ReceiptExtractionResult
            {
                Tarih = new DateTime(2026, 8, 10),
                ToplamTutar = 1800m,
                TahminiTur = ReceiptType.Bakim,
                GuvenSkoru = 0.8,
                KalemListesi = new List<ReceiptItemResult>
                {
                    new ReceiptItemResult { Ad = "MOTOR YAĞI 5W30", Tutar = 1800m }
                }
            };

            var taslakId = (await VeriAsync(await YukleAsync(sahip))).GetProperty("taslakId").GetInt32();

            await sahip.PostAsJsonAsync($"/api/Receipts/{taslakId}/confirm", new
            {
                vehicleId = aracId,
                tur = "Bakim",
                tarih = "2026-08-10",
                tutar = 1800m,
                km = 121000,
                bakimTuru = "YagDegisimi",
                parcalar = new object[]
                {
                    new { parcaTuru = "MotorYagi", aciklama = "5W30", adet = 1, tutar = 1800m, marka = (string)null },
                    new { parcaTuru = "Buji", aciklama = "buji", adet = 4, tutar = 0m, marka = (string)null }
                }
            });

            var taslak = await VeriAsync(await sahip.GetAsync($"/api/Receipts/{taslakId}"));
            var duzeltilen = taslak.GetProperty("duzeltilenAlanlar").GetString();

            Assert.Contains("parcalar", duzeltilen);
            Assert.Equal(1, duzeltilen.Split(',').Count(a => a.Trim() == "parcalar"));
        }

        public void Dispose()
        {
            _factory.Dispose();
            try { Directory.Delete(_klasor, true); } catch { }
        }
    }
}
