using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class ElektrikliAracHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public ElektrikliAracHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static string Plaka() => "34EV" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("ev"), fullName = "EV Sahip", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string yakitTuru, int km = 100000)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = Plaka(),
                brand = "Renault",
                model = "Zoe",
                year = 2022,
                currentKm = km,
                fuelType = yakitTuru
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task ElektrikliAracaSarjKaydiEklenir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "Elektrik");

            var veri = await VeriAsync(await sahip.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId,
                date = "2026-03-01",
                liters = 0m,
                totalCost = 350m,
                km = 100200,
                kwh = 42.5m,
                sarjTuru = "HizliSarj"
            }));

            Assert.Equal(42.5m, veri.GetProperty("kwh").GetDecimal());
            Assert.Equal("HizliSarj", veri.GetProperty("sarjTuru").GetString());
            Assert.Equal(0m, veri.GetProperty("liters").GetDecimal());
        }

        [Fact]
        public async Task ElektrikliAracaLitreGirilemez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "Elektrik");

            var cevap = await sahip.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId,
                date = "2026-03-01",
                liters = 40m,
                totalCost = 1900m,
                km = 100200
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task ElektrikliAracaSarjsizKayitReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "Elektrik");

            var cevap = await sahip.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId,
                date = "2026-03-01",
                liters = 0m,
                totalCost = 350m,
                km = 100200
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task BenzinliAracaSarjGirilemez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "Benzin");

            var cevap = await sahip.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId,
                date = "2026-03-01",
                liters = 40m,
                totalCost = 1900m,
                km = 100200,
                kwh = 10m
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task HibritAracHemLitreHemSarjKabulEder()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "Hibrit");

            var yakit = await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-01", liters = 30m, totalCost = 1400m, km = 100200 });
            var sarj = await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-05", liters = 0m, totalCost = 200m, km = 100400, kwh = 18m, sarjTuru = "Ev" });
            var ikisi = await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-10", liters = 20m, totalCost = 1100m, km = 100600, kwh = 12m, sarjTuru = "Isyeri" });

            Assert.Equal(HttpStatusCode.OK, yakit.StatusCode);
            Assert.Equal(HttpStatusCode.OK, sarj.StatusCode);
            Assert.Equal(HttpStatusCode.OK, ikisi.StatusCode);
        }

        [Fact]
        public async Task HibritAracBosKayitKabulEtmez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "Hibrit");

            var cevap = await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-01", liters = 0m, totalCost = 100m, km = 100200 });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task TanimsizSarjTuruReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "Elektrik");

            var cevap = await sahip.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId,
                date = "2026-03-01",
                liters = 0m,
                totalCost = 350m,
                km = 100200,
                kwh = 40m,
                sarjTuru = 9
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task MaliyetKwhTuketiminiHesaplar()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "Elektrik");

            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-01", liters = 0m, totalCost = 300m, km = 100000, kwh = 40m, sarjTuru = "Ev" });
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-15", liters = 0m, totalCost = 320m, km = 100400, kwh = 60m, sarjTuru = "Ev" });

            var veri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-03-01&bitis=2026-03-31"));

            Assert.Equal(100m, veri.GetProperty("toplamKwh").GetDecimal());
            Assert.Equal(15m, veri.GetProperty("kwh100Km").GetDecimal());
            Assert.Equal(JsonValueKind.Null, veri.GetProperty("litre100Km").ValueKind);

            var mart = veri.GetProperty("tuketimSeri").EnumerateArray().Single(t => t.GetProperty("ay").GetInt32() == 3);
            Assert.Equal(15m, mart.GetProperty("kwh100Km").GetDecimal());
        }

        [Fact]
        public async Task SarjKaydiCsvyeYazilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "Elektrik");
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-01", liters = 0m, totalCost = 350m, km = 100200, kwh = 42.5m, sarjTuru = "HizliSarj" });

            var metin = await (await sahip.GetAsync($"/api/Export/yakit.csv?vehicleId={aracId}")).Content.ReadAsStringAsync();

            Assert.Contains("Plaka;Tarih;Kilometre;Litre;BirimFiyat;Tutar;Kwh;SarjTuru", metin);
            Assert.Contains("42,50;HizliSarj", metin);
        }
    }
}
