using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class UzunMetinKirpmaTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public UzunMetinKirpmaTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";
        private static string Uzun(int n) => new string('A', n);

        private async Task<(HttpClient Client, int AracId)> HazirlaAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Kırpma Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(), brand = "Fiat", model = "Egea",
                year = 2020, currentKm = 10000, fuelType = "Benzin"
            });

            var id = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            return (client, id);
        }

        [Fact]
        public async Task BakimUzunMetinleriKirpar()
        {
            var (client, aracId) = await HazirlaAsync("bakim");

            var cevap = await client.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                date = DateTime.UtcNow.Date,
                km = 10500,
                cost = 1000m,
                serviceName = Uzun(5000),
                note = Uzun(5000),
                parcalar = new[] { new { parcaTuru = "YagFiltresi", aciklama = Uzun(5000), adet = 1, tutar = 100m, marka = Uzun(5000) } }
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var veri = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.Equal(150, veri.GetProperty("serviceName").GetString().Length);
            Assert.Equal(500, veri.GetProperty("note").GetString().Length);

            var parca = veri.GetProperty("parcalar").EnumerateArray().First();
            Assert.Equal(200, parca.GetProperty("aciklama").GetString().Length);
            Assert.Equal(100, parca.GetProperty("marka").GetString().Length);
        }

        [Fact]
        public async Task MasrafUzunNotuKirpar()
        {
            var (client, aracId) = await HazirlaAsync("masraf");

            var cevap = await client.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = aracId,
                category = "Otopark",
                date = DateTime.UtcNow.Date,
                amount = 100m,
                note = Uzun(5000)
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var veri = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            Assert.Equal(500, veri.GetProperty("note").GetString().Length);
        }

        [Fact]
        public async Task HatirlatmaUzunNotuKirpar()
        {
            var (client, aracId) = await HazirlaAsync("hatirlatma");

            var cevap = await client.PostAsJsonAsync("/api/Reminders", new
            {
                vehicleId = aracId,
                type = "Muayene",
                dueDate = DateTime.UtcNow.Date.AddMonths(2),
                note = Uzun(5000)
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var veri = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            Assert.Equal(500, veri.GetProperty("note").GetString().Length);
        }

        [Fact]
        public async Task BakimGuncellemeDeKirpar()
        {
            var (client, aracId) = await HazirlaAsync("guncelle");

            var ekle = await client.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = aracId, type = "PeriyodikBakim", date = DateTime.UtcNow.Date,
                km = 10500, cost = 1000m, serviceName = "Kısa", note = "Kısa"
            });

            var id = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var cevap = await client.PutAsJsonAsync($"/api/Maintenance/{id}", new
            {
                type = "PeriyodikBakim", date = DateTime.UtcNow.Date, km = 10600,
                cost = 1000m, serviceName = Uzun(5000), note = Uzun(5000)
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var veri = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            Assert.Equal(150, veri.GetProperty("serviceName").GetString().Length);
        }
        [Fact]
        public async Task SifirTutarliMasrafReddedilir()
        {
            var (client, aracId) = await HazirlaAsync("sifir");

            var cevap = await client.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = aracId, category = "Otopark",
                date = DateTime.UtcNow.Date, amount = 0m, note = "sıfır"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

    }
}
