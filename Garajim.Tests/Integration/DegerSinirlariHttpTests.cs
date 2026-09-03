using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class DegerSinirlariHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public DegerSinirlariHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static string Bugun() => DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        private static string Yarin(int gun) => DateTime.UtcNow.Date.AddDays(gun).ToString("yyyy-MM-dd");

        private async Task<(HttpClient Client, int AracId)> AracliSahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Sınır Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Fiat", model = "Egea", year = 2020, currentKm = 50000,
                fuelType = "Dizel", vites = "Manuel", kasaTipi = "Sedan"
            });
            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            return (client, aracId);
        }

        [Fact]
        public async Task AsiriYilReddedilir()
        {
            var (client, _) = await AracliSahipAsync("yil");

            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Fiat", model = "Egea", year = DateTime.UtcNow.Year + 5, currentKm = 1000,
                fuelType = "Dizel", vites = "Manuel", kasaTipi = "Sedan"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task AsiriKilometreReddedilir()
        {
            var (client, _) = await AracliSahipAsync("km");

            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Fiat", model = "Egea", year = 2020, currentKm = 2_000_001,
                fuelType = "Dizel", vites = "Manuel", kasaTipi = "Sedan"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task AsiriTutarReddedilir()
        {
            var (client, aracId) = await AracliSahipAsync("tutar");

            var cevap = await client.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = aracId, date = Bugun(), category = "Otopark", amount = 5_000_001m, note = "Aşırı"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task AsiriLitreReddedilir()
        {
            var (client, aracId) = await AracliSahipAsync("litre");

            var cevap = await client.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId, date = Bugun(), km = 51000, liters = 1501m, totalCost = 1000m
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Theory]
        [InlineData("/api/Fuel")]
        [InlineData("/api/Maintenance")]
        [InlineData("/api/Expenses")]
        [InlineData("/api/Yolculuk")]
        public async Task GelecekTarihliKayitReddedilir(string yol)
        {
            var (client, aracId) = await AracliSahipAsync("gelecek");
            var tarih = Yarin(5);

            object govde = yol switch
            {
                "/api/Fuel" => new { vehicleId = aracId, date = tarih, km = 51000, liters = 40m, totalCost = 2000m },
                "/api/Maintenance" => new { vehicleId = aracId, date = tarih, type = "PeriyodikBakim", km = 51000, cost = 3000m },
                "/api/Expenses" => new { vehicleId = aracId, date = tarih, category = "Otopark", amount = 100m, note = "İleri" },
                _ => new { vehicleId = aracId, tarih, baslangicKm = 51000, bitisKm = 51100, amac = "Is" }
            };

            var cevap = await client.PostAsJsonAsync(yol, govde);

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task BugunkuKayitKabulEdilir()
        {
            var (client, aracId) = await AracliSahipAsync("bugun");

            var cevap = await client.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = aracId, date = Bugun(), category = "Otopark", amount = 100m, note = "Bugün"
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task EvrakBitisTarihiGelecekOlabilir()
        {
            var (client, aracId) = await AracliSahipAsync("evrak");

            var cevap = await client.PostAsJsonAsync("/api/Evrak", new
            {
                vehicleId = aracId, evrakTuru = "TrafikSigortasi", bitisTarihi = Yarin(200), saglayici = "Örnek", policeNo = "P-1"
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }
    }
}
