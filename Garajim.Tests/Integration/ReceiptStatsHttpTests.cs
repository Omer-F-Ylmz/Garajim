using System.Net;
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
    public class ReceiptStatsHttpTests : IDisposable
    {
        private sealed class SahteExtractor : IReceiptExtractor
        {
            public ReceiptExtractionResult Sonuc { get; set; } = new ReceiptExtractionResult();

            public Task<ReceiptExtractionResult> ExtractAsync(byte[] imageBytes, string mimeType, CancellationToken ct)
            {
                return Task.FromResult(Sonuc);
            }
        }

        private sealed class IstatistikFactory : GarajimWebApplicationFactory
        {
            private readonly string _klasor;

            public SahteExtractor Extractor { get; } = new SahteExtractor();

            public IstatistikFactory(string klasor)
            {
                _klasor = klasor;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Documents:StoragePath"] = _klasor
                    });
                });

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
        private readonly IstatistikFactory _factory;

        public ReceiptStatsHttpTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-fis-ist-" + Guid.NewGuid().ToString("N"));
            _factory = new IstatistikFactory(_klasor);
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "İstatistik Sahibi", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<HttpClient> YoneticiOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("manager");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Yönetici", role = "Manager" });
            var sifre = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("temporaryPassword").GetString();

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = sifre });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
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

        private static Task<HttpResponseMessage> YukleAsync(HttpClient client)
        {
            var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(PngIcerik);
            dosya.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(dosya, "file", "fis.png");
            return client.PostAsync("/api/Receipts", form);
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task IstatistikOnayRedVeDoldurmaOranlariniVerir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34IST001");

            _factory.Extractor.Sonuc = new ReceiptExtractionResult
            {
                Tarih = new DateTime(2026, 8, 20),
                ToplamTutar = 1000m,
                Km = 100500,
                TahminiTur = ReceiptType.Yakit,
                GuvenSkoru = 0.8
            };
            var onaylanacak = (await VeriAsync(await YukleAsync(sahip))).GetProperty("id").GetInt32();

            _factory.Extractor.Sonuc = new ReceiptExtractionResult { GuvenSkoru = 0.2 };
            var reddedilecek = (await VeriAsync(await YukleAsync(sahip))).GetProperty("id").GetInt32();

            await sahip.PostAsJsonAsync($"/api/Receipts/{onaylanacak}/confirm", new
            {
                vehicleId = aracId,
                tur = "Yakit",
                tarih = "2026-08-20",
                tutar = 1250m,
                km = 100500,
                litre = 25m
            });
            await sahip.PostAsync($"/api/Receipts/{reddedilecek}/reject", null);

            var istatistik = await VeriAsync(await sahip.GetAsync("/api/Receipts/stats"));

            Assert.Equal(2, istatistik.GetProperty("toplamCagri").GetInt32());
            Assert.Equal(1, istatistik.GetProperty("onaylanan").GetInt32());
            Assert.Equal(1, istatistik.GetProperty("reddedilen").GetInt32());
            Assert.Equal(0, istatistik.GetProperty("bekleyen").GetInt32());
            Assert.Equal(50, istatistik.GetProperty("onayOrani").GetDouble(), 1);
            Assert.Equal(0.5, istatistik.GetProperty("ortalamaGuven").GetDouble(), 3);
        }

        [Fact]
        public async Task AlanDolulukYuzdeleriHesaplanir()
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "34IST002");

            _factory.Extractor.Sonuc = new ReceiptExtractionResult
            {
                Tarih = new DateTime(2026, 8, 20),
                ToplamTutar = 1000m,
                GuvenSkoru = 0.9
            };
            await YukleAsync(sahip);

            _factory.Extractor.Sonuc = new ReceiptExtractionResult { GuvenSkoru = 0 };
            await YukleAsync(sahip);

            var doluluk = (await VeriAsync(await sahip.GetAsync("/api/Receipts/stats"))).GetProperty("alanDoluluk");

            Assert.Equal(50, doluluk.GetProperty("tarih").GetDouble(), 1);
            Assert.Equal(50, doluluk.GetProperty("toplamTutar").GetDouble(), 1);
            Assert.Equal(0, doluluk.GetProperty("km").GetDouble(), 1);
        }

        [Fact]
        public async Task AlanBazindaDuzeltmeOraniOnaylananlardanHesaplanir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34IST003");

            _factory.Extractor.Sonuc = new ReceiptExtractionResult
            {
                Tarih = new DateTime(2026, 8, 20),
                ToplamTutar = 1000m,
                Km = 100500,
                TahminiTur = ReceiptType.Yakit,
                GuvenSkoru = 0.8
            };

            var birinci = (await VeriAsync(await YukleAsync(sahip))).GetProperty("id").GetInt32();
            var ikinci = (await VeriAsync(await YukleAsync(sahip))).GetProperty("id").GetInt32();

            await sahip.PostAsJsonAsync($"/api/Receipts/{birinci}/confirm", new
            {
                vehicleId = aracId,
                tur = "Yakit",
                tarih = "2026-08-20",
                tutar = 9999m,
                km = 100500
            });

            await sahip.PostAsJsonAsync($"/api/Receipts/{ikinci}/confirm", new
            {
                vehicleId = aracId,
                tur = "Yakit",
                tarih = "2026-08-20",
                tutar = 1000m,
                km = 100500
            });

            var duzeltme = (await VeriAsync(await sahip.GetAsync("/api/Receipts/stats"))).GetProperty("alanDuzeltmeOrani");

            Assert.Equal(50, duzeltme.GetProperty("tutar").GetDouble(), 1);
            Assert.Equal(0, duzeltme.GetProperty("tarih").GetDouble(), 1);
            Assert.Equal(0, duzeltme.GetProperty("km").GetDouble(), 1);
        }

        [Fact]
        public async Task IstatistikYalnizOwnerErisebilir()
        {
            var sahip = await SahipOlusturAsync();
            var yonetici = await YoneticiOlusturAsync(sahip);

            var cevap = await yonetici.GetAsync("/api/Receipts/stats");

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }

        [Fact]
        public async Task KayitYokkenIstatistikSifirlarlaDoner()
        {
            var sahip = await SahipOlusturAsync();

            var istatistik = await VeriAsync(await sahip.GetAsync("/api/Receipts/stats"));

            Assert.Equal(0, istatistik.GetProperty("toplamCagri").GetInt32());
            Assert.Equal(0, istatistik.GetProperty("onayOrani").GetDouble(), 1);
            Assert.Equal(0, istatistik.GetProperty("ortalamaGuven").GetDouble(), 3);
        }

        public void Dispose()
        {
            _factory.Dispose();
            try
            {
                if (Directory.Exists(_klasor))
                {
                    Directory.Delete(_klasor, true);
                }
            }
            catch
            {
            }
        }
    }
}
