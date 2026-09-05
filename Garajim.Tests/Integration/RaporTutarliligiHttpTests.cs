using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class RaporTutarliligiHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public RaporTutarliligiHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Rapor", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka, string yakit = "Dizel")
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Fiat",
                model = "Egea",
                year = 2019,
                currentKm = 50000,
                fuelType = yakit
            });

            var govde = await cevap.Content.ReadAsStringAsync();
            Assert.True(cevap.IsSuccessStatusCode, govde);
            return JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task YakitEkleAsync(HttpClient client, int aracId, int km, decimal litre, decimal tutar, bool tamDolum, int gunOnce)
        {
            var cevap = await client.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId,
                date = DateTime.UtcNow.Date.AddDays(-gunOnce),
                km,
                liters = litre,
                totalCost = tutar,
                tamDolum
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task YakitIstatistigiKendiIcindeTutarlidir()
        {
            var client = await SahipAsync("raportutar");
            var aracId = await AracEkleAsync(client, "34RT1001");

            await YakitEkleAsync(client, aracId, 50000, 40m, 2000m, true, 30);
            await YakitEkleAsync(client, aracId, 50500, 20m, 1000m, false, 20);
            await YakitEkleAsync(client, aracId, 51000, 30m, 1500m, true, 10);

            var cevap = await client.GetAsync("/api/Reports/fuel-stats?vehicleId=" + aracId);
            var veri = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var toplamKm = veri.GetProperty("totalKm").GetInt32();
            var toplamTutar = veri.GetProperty("totalCost").GetDecimal();
            var kmBasina = veri.GetProperty("costPerKm").GetDecimal();

            Assert.Equal(Math.Round(toplamTutar / toplamKm, 2), kmBasina);
        }

        [Fact]
        public async Task ElektrikliAracLitreDegilKwhBildirir()
        {
            var client = await SahipAsync("raporelektrik");
            var aracId = await AracEkleAsync(client, "34RT1002", "Elektrik");

            for (var i = 0; i < 3; i++)
            {
                var cevap = await client.PostAsJsonAsync("/api/Fuel", new
                {
                    vehicleId = aracId,
                    date = DateTime.UtcNow.Date.AddDays(-10 + i),
                    km = 50000 + i * 500,
                    liters = 0,
                    kwh = 40m,
                    sarjTuru = "Ev",
                    totalCost = 500m,
                    tamDolum = true
                });

                Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
            }

            var stats = await client.GetAsync("/api/Reports/fuel-stats?vehicleId=" + aracId);
            var veri = JsonDocument.Parse(await stats.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.True(veri.GetProperty("elektrikli").GetBoolean());
            Assert.Null(veri.GetProperty("averageConsumptionPer100Km").GetDecimal() is 0m ? null : "sıfır litre bildirilmemeli");
        }

        [Fact]
        public async Task ArsivliAracFiloKarsilastirmasindaGorunmez()
        {
            var client = await SahipAsync("raporarsiv");
            var aktifId = await AracEkleAsync(client, "34RT1003");
            var arsivId = await AracEkleAsync(client, "34RT1004");

            await client.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = arsivId,
                category = "Otopark",
                date = DateTime.UtcNow.Date.AddDays(-2),
                amount = 500m
            });

            var arsivle = await client.PostAsJsonAsync("/api/Vehicles/" + arsivId + "/arsiv", new { neden = "Satildi" });
            Assert.True(arsivle.IsSuccessStatusCode, await arsivle.Content.ReadAsStringAsync());

            var bas = DateTime.UtcNow.Date.AddMonths(-1).ToString("yyyy-MM-dd");
            var son = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

            var cevap = await client.GetAsync($"/api/Reports/filo-maliyet?start={bas}&end={son}");
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Contains("34RT1003", govde);
            Assert.DoesNotContain("34RT1004", govde);
            Assert.True(aktifId > 0);
        }
    }
}
