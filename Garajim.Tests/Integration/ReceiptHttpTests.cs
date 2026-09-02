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
    public class ReceiptHttpTests : IDisposable
    {
        private sealed class SahteExtractor : IReceiptExtractor
        {
            public ReceiptExtractionResult Sonuc { get; set; } = new ReceiptExtractionResult();

            public Task<ReceiptExtractionResult> ExtractAsync(byte[] imageBytes, string mimeType, CancellationToken ct)
            {
                return Task.FromResult(Sonuc);
            }
        }

        private sealed class FisFactory : GarajimWebApplicationFactory
        {
            private readonly string _klasor;
            private readonly int _aylikLimit;

            public SahteExtractor Extractor { get; } = new SahteExtractor();

            public FisFactory(string klasor, int aylikLimit)
            {
                _klasor = klasor;
                _aylikLimit = aylikLimit;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Documents:StoragePath"] = _klasor,
                        ["Receipts:AylikLimit"] = _aylikLimit.ToString()
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var kayitlar = services.Where(s => s.ServiceType == typeof(IReceiptExtractor)).ToList();
                    foreach (var kayit in kayitlar)
                    {
                        services.Remove(kayit);
                    }

                    services.AddSingleton<IReceiptExtractor>(Extractor);
                });
            }
        }

        private static readonly byte[] PngIcerik = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };

        private readonly string _klasor;
        private readonly FisFactory _factory;

        public ReceiptHttpTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-fis-" + Guid.NewGuid().ToString("N"));
            _factory = new FisFactory(_klasor, 100);
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static ReceiptExtractionResult DoluSonuc(string plaka = "34FIS001")
        {
            return new ReceiptExtractionResult
            {
                Tarih = new DateTime(2026, 8, 20),
                ToplamTutar = 1980.50m,
                KdvTutari = 330.08m,
                Litre = 42.5m,
                BirimFiyat = 46.60m,
                Plaka = plaka,
                Km = 123000,
                TahminiTur = ReceiptType.Yakit,
                GuvenSkoru = 0.9,
                KalemListesi = new List<ReceiptItemResult> { new ReceiptItemResult { Ad = "KURŞUNSUZ 95", Tutar = 1980.50m } }
            };
        }

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "Fiş Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("driver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Fiş Sürücüsü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            var sifre = veri.GetProperty("temporaryPassword").GetString();
            var userId = veri.GetProperty("userId").GetInt32();

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = sifre });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, userId);
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
        public async Task YuklemeDoluTaslakDondururVePlakadanAracOnerir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34 FIS 001");
            _factory.Extractor.Sonuc = DoluSonuc();

            var cevap = await YukleAsync(sahip);
            var zarf = await VeriAsync(cevap);
            var veri = zarf.GetProperty("taslak");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal("Bekliyor", zarf.GetProperty("durum").GetString());
            Assert.Equal(1980.50m, veri.GetProperty("toplamTutar").GetDecimal());
            Assert.Equal("34FIS001", veri.GetProperty("plaka").GetString());
            Assert.Equal(aracId, veri.GetProperty("vehicleId").GetInt32());
            Assert.Equal("Yakit", veri.GetProperty("tahminiTur").GetString());
            Assert.Equal(123000, veri.GetProperty("km").GetInt32());
        }

        [Fact]
        public async Task OnayYakitKaydiVeBelgeOlusturur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34FIS001");
            _factory.Extractor.Sonuc = DoluSonuc();

            var taslakId = (await VeriAsync(await YukleAsync(sahip))).GetProperty("taslakId").GetInt32();

            var onay = await sahip.PostAsJsonAsync($"/api/Receipts/{taslakId}/confirm", new
            {
                vehicleId = aracId,
                tur = "Yakit",
                tarih = "2026-08-20",
                tutar = 2000.00m,
                litre = 42.5m,
                km = 123456
            });

            Assert.Equal(HttpStatusCode.OK, onay.StatusCode);

            var yakitlar = await VeriAsync(await sahip.GetAsync($"/api/Fuel?vehicleId={aracId}"));
            var kayit = Assert.Single(yakitlar.EnumerateArray());
            Assert.Equal(2000.00m, kayit.GetProperty("totalCost").GetDecimal());
            Assert.Equal(123456, kayit.GetProperty("km").GetInt32());

            var belgeler = await VeriAsync(await sahip.GetAsync($"/api/Documents?vehicleId={aracId}"));
            var belge = Assert.Single(belgeler.EnumerateArray());
            Assert.Equal("fis.png", belge.GetProperty("originalName").GetString());

            var taslak = await VeriAsync(await sahip.GetAsync($"/api/Receipts/{taslakId}"));
            Assert.Equal("Onaylandi", taslak.GetProperty("durum").GetString());
            var duzeltilen = taslak.GetProperty("duzeltilenAlanlar").GetString();
            Assert.Contains("Tutar", duzeltilen);
            Assert.Contains("Km", duzeltilen);
            Assert.DoesNotContain("Tarih", duzeltilen);
        }

        [Fact]
        public async Task OnayBakimTurundeBakimKaydiOlusturur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34FIS002");
            _factory.Extractor.Sonuc = new ReceiptExtractionResult
            {
                Tarih = new DateTime(2026, 8, 18),
                ToplamTutar = 4500m,
                TahminiTur = ReceiptType.Bakim,
                GuvenSkoru = 0.8
            };

            var taslakId = (await VeriAsync(await YukleAsync(sahip))).GetProperty("taslakId").GetInt32();

            var onay = await sahip.PostAsJsonAsync($"/api/Receipts/{taslakId}/confirm", new
            {
                vehicleId = aracId,
                tur = "Bakim",
                tarih = "2026-08-18",
                tutar = 4500m,
                km = 121000,
                bakimTuru = "YagDegisimi",
                not = "Yağ ve filtre"
            });

            Assert.Equal(HttpStatusCode.OK, onay.StatusCode);

            var bakimlar = await VeriAsync(await sahip.GetAsync($"/api/Maintenance?vehicleId={aracId}"));
            var kayit = Assert.Single(bakimlar.EnumerateArray());
            Assert.Equal("YagDegisimi", kayit.GetProperty("type").GetString());
            Assert.Equal(4500m, kayit.GetProperty("cost").GetDecimal());
        }

        [Fact]
        public async Task ReddetTaslagiKapatirDosyayiSiler()
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "34FIS003");
            _factory.Extractor.Sonuc = DoluSonuc("34FIS003");

            var taslakId = (await VeriAsync(await YukleAsync(sahip))).GetProperty("taslakId").GetInt32();
            Assert.Single(Directory.GetFiles(_klasor));

            var red = await sahip.PostAsync($"/api/Receipts/{taslakId}/reject", null);

            Assert.Equal(HttpStatusCode.OK, red.StatusCode);
            Assert.Empty(Directory.GetFiles(_klasor));

            var taslak = await VeriAsync(await sahip.GetAsync($"/api/Receipts/{taslakId}"));
            Assert.Equal("Reddedildi", taslak.GetProperty("durum").GetString());
        }

        [Fact]
        public async Task AylikLimitAsimindaYuzYirmiDokuzDoner()
        {
            var klasor = Path.Combine(Path.GetTempPath(), "garajim-fis-" + Guid.NewGuid().ToString("N"));
            using var limitliFactory = new FisFactory(klasor, 2);
            limitliFactory.Extractor.Sonuc = new ReceiptExtractionResult { GuvenSkoru = 0.5 };

            var client = limitliFactory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("limit"), fullName = "Limit Testi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            Assert.Equal(HttpStatusCode.OK, (await YukleClientAsync(client)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await YukleClientAsync(client)).StatusCode);

            var ucuncu = await YukleClientAsync(client);
            var govde = await ucuncu.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.TooManyRequests, ucuncu.StatusCode);
            Assert.Contains("limit", govde, StringComparison.OrdinalIgnoreCase);

            try { Directory.Delete(klasor, true); } catch { }
        }

        private static Task<HttpResponseMessage> YukleClientAsync(HttpClient client)
        {
            var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(PngIcerik);
            dosya.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(dosya, "file", "fis.png");
            return client.PostAsync("/api/Receipts", form);
        }

        [Fact]
        public async Task DriverZimmetsizAracaOnaylayamaz()
        {
            var sahip = await SahipOlusturAsync();
            var zimmetliArac = await AracEkleAsync(sahip, "34FIS004");
            var zimmetsizArac = await AracEkleAsync(sahip, "34FIS005");
            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = zimmetliArac, userId = surucuId });

            _factory.Extractor.Sonuc = new ReceiptExtractionResult { ToplamTutar = 500m, TahminiTur = ReceiptType.Masraf, GuvenSkoru = 0.7 };
            var taslakId = (await VeriAsync(await YukleAsync(surucu))).GetProperty("taslakId").GetInt32();

            var yasak = await surucu.PostAsJsonAsync($"/api/Receipts/{taslakId}/confirm", new
            {
                vehicleId = zimmetsizArac,
                tur = "Masraf",
                tarih = "2026-08-25",
                tutar = 500m,
                masrafKategorisi = "Otopark"
            });

            Assert.Equal(HttpStatusCode.NotFound, yasak.StatusCode);

            var izinli = await surucu.PostAsJsonAsync($"/api/Receipts/{taslakId}/confirm", new
            {
                vehicleId = zimmetliArac,
                tur = "Masraf",
                tarih = "2026-08-25",
                tutar = 500m,
                masrafKategorisi = "Otopark"
            });

            Assert.Equal(HttpStatusCode.OK, izinli.StatusCode);
        }

        [Fact]
        public async Task YabanciSirketinTaslagi404Doner()
        {
            var birinci = await SahipOlusturAsync();
            await AracEkleAsync(birinci, "34FIS006");
            _factory.Extractor.Sonuc = DoluSonuc("34FIS006");
            var taslakId = (await VeriAsync(await YukleAsync(birinci))).GetProperty("taslakId").GetInt32();

            var ikinci = await SahipOlusturAsync();

            var cevap = await ikinci.GetAsync($"/api/Receipts/{taslakId}");

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task DusukGuvenliSonucAlanlariBosAma200Doner()
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "34FIS007");
            _factory.Extractor.Sonuc = new ReceiptExtractionResult { GuvenSkoru = 0 };

            var cevap = await YukleAsync(sahip);
            var zarf = await VeriAsync(cevap);
            var veri = zarf.GetProperty("taslak");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal(JsonValueKind.Null, veri.GetProperty("tarih").ValueKind);
            Assert.Equal(JsonValueKind.Null, veri.GetProperty("toplamTutar").ValueKind);
            Assert.Equal(JsonValueKind.Null, veri.GetProperty("vehicleId").ValueKind);
            Assert.Equal("Bekliyor", zarf.GetProperty("durum").GetString());
        }

        [Fact]
        public async Task BekleyenlerDurumFiltresiyleListelenir()
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "34FIS008");
            _factory.Extractor.Sonuc = DoluSonuc("34FIS008");

            var birinciId = (await VeriAsync(await YukleAsync(sahip))).GetProperty("taslakId").GetInt32();
            var ikinciId = (await VeriAsync(await YukleAsync(sahip))).GetProperty("taslakId").GetInt32();
            await sahip.PostAsync($"/api/Receipts/{birinciId}/reject", null);

            var bekleyenler = await VeriAsync(await sahip.GetAsync("/api/Receipts?durum=Bekliyor"));
            var tek = Assert.Single(bekleyenler.EnumerateArray());

            Assert.Equal(ikinciId, tek.GetProperty("id").GetInt32());
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
