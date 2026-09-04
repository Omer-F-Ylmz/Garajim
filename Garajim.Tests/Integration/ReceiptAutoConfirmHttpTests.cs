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
    public class ReceiptAutoConfirmHttpTests : IDisposable
    {
        private sealed class SahteExtractor : IReceiptExtractor
        {
            public ReceiptExtractionResult Sonuc { get; set; } = new ReceiptExtractionResult();

            public Task<ReceiptExtractionResult> ExtractAsync(byte[] imageBytes, string mimeType, CancellationToken ct)
            {
                return Task.FromResult(Sonuc);
            }
        }

        private sealed class OtoFactory : GarajimWebApplicationFactory
        {
            private readonly string _klasor;
            private readonly int _aylikLimit;

            public SahteExtractor Extractor { get; } = new SahteExtractor();

            public OtoFactory(string klasor, int aylikLimit = 100)
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
        private readonly OtoFactory _factory;

        public ReceiptAutoConfirmHttpTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-oto-" + Guid.NewGuid().ToString("N"));
            _factory = new OtoFactory(_klasor);
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static ReceiptExtractionResult TamSonuc(string plaka, double guven = 0.92)
        {
            return new ReceiptExtractionResult
            {
                Tarih = new DateTime(2026, 8, 20),
                ToplamTutar = 1980.50m,
                Litre = 42.5m,
                BirimFiyat = 46.60m,
                Plaka = plaka,
                Km = 123000,
                TahminiTur = ReceiptType.Yakit,
                GuvenSkoru = guven
            };
        }

        private async Task<HttpClient> SahipOlusturAsync(OtoFactory factory = null)
        {
            var f = factory ?? _factory;
            var client = f.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "Oto Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("driver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Oto Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
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

        private static Task<HttpResponseMessage> YukleAsync(HttpClient client, bool otoOnay)
        {
            var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(PngIcerik);
            dosya.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(dosya, "file", "fis.png");
            return client.PostAsync("/api/Receipts?otoOnay=" + (otoOnay ? "true" : "false"), form);
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task UcuTamamsaOtoOnaylanirKayitVeBelgeOlusur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34OTO001");
            _factory.Extractor.Sonuc = TamSonuc("34OTO001");

            var cevap = await YukleAsync(sahip, true);
            var veri = await VeriAsync(cevap);

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal("Onaylandi", veri.GetProperty("durum").GetString());
            Assert.Equal(JsonValueKind.Null, veri.GetProperty("atlamaNedeni").ValueKind);
            Assert.Equal("Yakit", veri.GetProperty("olusturulanKayit").GetProperty("tur").GetString());
            Assert.True(veri.GetProperty("olusturulanKayit").GetProperty("id").GetInt32() > 0);

            var taslakId = veri.GetProperty("taslakId").GetInt32();
            var taslak = await VeriAsync(await sahip.GetAsync($"/api/Receipts/{taslakId}"));
            Assert.True(taslak.GetProperty("otoOnaylandi").GetBoolean());
            Assert.True(string.IsNullOrEmpty(taslak.GetProperty("duzeltilenAlanlar").GetString()));

            var yakitlar = await VeriAsync(await sahip.GetAsync($"/api/Fuel?vehicleId={aracId}"));
            var kayit = Assert.Single(yakitlar.EnumerateArray());
            Assert.Equal(1980.50m, kayit.GetProperty("totalCost").GetDecimal());
            Assert.Equal(123000, kayit.GetProperty("km").GetInt32());

            var belgeler = await VeriAsync(await sahip.GetAsync($"/api/Documents?vehicleId={aracId}"));
            Assert.Single(belgeler.EnumerateArray());
        }

        [Fact]
        public async Task EsikAltiGuvendeBekliyorKalir()
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "34OTO002");
            _factory.Extractor.Sonuc = TamSonuc("34OTO002", 0.60);

            var veri = await VeriAsync(await YukleAsync(sahip, true));

            Assert.Equal("Bekliyor", veri.GetProperty("durum").GetString());
            Assert.Contains("güven", veri.GetProperty("atlamaNedeni").GetString(), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(JsonValueKind.Null, veri.GetProperty("olusturulanKayit").ValueKind);
        }

        [Theory]
        [InlineData("35 EZ 7721")]
        [InlineData("35 ez 7721")]
        [InlineData("35-EZ-7721")]
        [InlineData("  35ez7721  ")]
        public async Task FistekiPlakaNormalizeEdilerekEslesir(string fistekiPlaka)
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "35EZ7721");
            _factory.Extractor.Sonuc = TamSonuc(fistekiPlaka);

            var veri = await VeriAsync(await YukleAsync(sahip, false));

            Assert.Equal(aracId, veri.GetProperty("taslak").GetProperty("vehicleId").GetInt32());
        }

        [Theory]
        [InlineData("35 EZ 7721")]
        [InlineData("35 ez 7721")]
        [InlineData("35-EZ-7721")]
        public async Task NormalizePlakaOtoOnayiAcar(string fistekiPlaka)
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "35EZ7721");
            _factory.Extractor.Sonuc = TamSonuc(fistekiPlaka);

            var veri = await VeriAsync(await YukleAsync(sahip, true));

            Assert.Equal("Onaylandi", veri.GetProperty("durum").GetString());
            Assert.Equal(JsonValueKind.Null, veri.GetProperty("atlamaNedeni").ValueKind);
        }

        [Theory]
        [InlineData("35 EZ 77ı1")]
        [InlineData("35 EZ 7Ş21")]
        [InlineData("OKUNAMADI!")]
        [InlineData("35/EZ/7721")]
        public async Task NormalizeEdilemeyenPlakaEslesmez(string fistekiPlaka)
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "35EZ7721");
            _factory.Extractor.Sonuc = TamSonuc(fistekiPlaka);

            var veri = await VeriAsync(await YukleAsync(sahip, true));

            Assert.Equal(JsonValueKind.Null, veri.GetProperty("taslak").GetProperty("vehicleId").ValueKind);
            Assert.Equal("Bekliyor", veri.GetProperty("durum").GetString());
            Assert.Contains("plaka", veri.GetProperty("atlamaNedeni").GetString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PlakaEslesmezseBekliyorVeNedenDoner()
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "34OTO003");
            _factory.Extractor.Sonuc = TamSonuc("06BASKA99");

            var veri = await VeriAsync(await YukleAsync(sahip, true));

            Assert.Equal("Bekliyor", veri.GetProperty("durum").GetString());
            Assert.Contains("plaka", veri.GetProperty("atlamaNedeni").GetString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task EksikAlanVarsaBekliyorKalir()
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "34OTO004");
            var sonuc = TamSonuc("34OTO004");
            sonuc.ToplamTutar = null;
            _factory.Extractor.Sonuc = sonuc;

            var veri = await VeriAsync(await YukleAsync(sahip, true));

            Assert.Equal("Bekliyor", veri.GetProperty("durum").GetString());
            Assert.Contains("tutar", veri.GetProperty("atlamaNedeni").GetString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DriverZimmetsizPlakadaOtoOnaylanmaz()
        {
            var sahip = await SahipOlusturAsync();
            var zimmetli = await AracEkleAsync(sahip, "34OTO005");
            await AracEkleAsync(sahip, "34OTO006");
            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = zimmetli, userId = surucuId });

            _factory.Extractor.Sonuc = TamSonuc("34OTO006");

            var veri = await VeriAsync(await YukleAsync(surucu, true));

            Assert.Equal("Bekliyor", veri.GetProperty("durum").GetString());
            Assert.Contains("plaka", veri.GetProperty("atlamaNedeni").GetString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task OtoOnayYokkenDavranisEskisiGibi()
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "34OTO007");
            _factory.Extractor.Sonuc = TamSonuc("34OTO007");

            var veri = await VeriAsync(await YukleAsync(sahip, false));

            Assert.Equal("Bekliyor", veri.GetProperty("durum").GetString());
            Assert.Equal(JsonValueKind.Null, veri.GetProperty("atlamaNedeni").ValueKind);
        }

        [Fact]
        public async Task AylikLimitOtoOnaydaDaSayilir()
        {
            var klasor = Path.Combine(Path.GetTempPath(), "garajim-oto-" + Guid.NewGuid().ToString("N"));
            using var limitli = new OtoFactory(klasor, 1);
            var sahip = await SahipOlusturAsync(limitli);
            await AracEkleAsync(sahip, "34OTO008");
            limitli.Extractor.Sonuc = TamSonuc("34OTO008");

            var form1 = new MultipartFormDataContent();
            var d1 = new ByteArrayContent(PngIcerik);
            d1.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form1.Add(d1, "file", "fis.png");
            Assert.Equal(HttpStatusCode.OK, (await sahip.PostAsync("/api/Receipts?otoOnay=true", form1)).StatusCode);

            var form2 = new MultipartFormDataContent();
            var d2 = new ByteArrayContent(PngIcerik);
            d2.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form2.Add(d2, "file", "fis.png");
            var ikinci = await sahip.PostAsync("/api/Receipts?otoOnay=true", form2);

            Assert.Equal(HttpStatusCode.TooManyRequests, ikinci.StatusCode);

            try { Directory.Delete(klasor, true); } catch { }
        }

        [Fact]
        public async Task StatsOtoOnaylananiAyirirVeDuzeltmeOraniElleOnaylananlardan()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34OTO009");

            _factory.Extractor.Sonuc = TamSonuc("34OTO009");
            await YukleAsync(sahip, true);

            _factory.Extractor.Sonuc = TamSonuc("34OTO009", 0.50);
            var elleId = (await VeriAsync(await YukleAsync(sahip, true))).GetProperty("taslakId").GetInt32();

            await sahip.PostAsJsonAsync($"/api/Receipts/{elleId}/confirm", new
            {
                vehicleId = aracId,
                tur = "Yakit",
                tarih = "2026-08-20",
                tutar = 9999m,
                km = 123000,
                litre = 42.5m
            });

            var istatistik = await VeriAsync(await sahip.GetAsync("/api/Receipts/stats"));

            Assert.Equal(2, istatistik.GetProperty("toplamCagri").GetInt32());
            Assert.Equal(2, istatistik.GetProperty("onaylanan").GetInt32());
            Assert.Equal(1, istatistik.GetProperty("otoOnaylanan").GetInt32());
            Assert.Equal(100, istatistik.GetProperty("alanDuzeltmeOrani").GetProperty("tutar").GetDouble(), 1);
            Assert.Equal(0, istatistik.GetProperty("alanDuzeltmeOrani").GetProperty("tarih").GetDouble(), 1);
        }

        public void Dispose()
        {
            _factory.Dispose();
            try { Directory.Delete(_klasor, true); } catch { }
        }
        [Fact]
        public async Task HizmetDoluysaTaslakOlusmazVe503Doner()
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "34DOL001");
            _factory.Extractor.Sonuc = new ReceiptExtractionResult { HizmetDolu = true };

            var cevap = await YukleAsync(sahip, false);
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.ServiceUnavailable, cevap.StatusCode);
            Assert.Contains("dolu", govde, StringComparison.OrdinalIgnoreCase);

            var taslaklar = await VeriAsync(await sahip.GetAsync("/api/Receipts"));
            Assert.Equal(0, taslaklar.GetArrayLength());
        }

    }
}
