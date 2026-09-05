using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class AlanBazliHataHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public AlanBazliHataHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int AracId)> HazirlaAsync(string on, string plaka)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Hata", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Fiat",
                model = "Egea",
                year = 2019,
                currentKm = 40000,
                fuelType = "Dizel"
            });

            var id = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            return (client, id);
        }

        private static async Task<string> MesajAl(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync())
                .RootElement.GetProperty("message").GetString();
        }

        [Fact]
        public async Task GelecekTarihliMasrafAcikMesajDoner()
        {
            var (client, aracId) = await HazirlaAsync("hatatarih", "34HB1001");

            var cevap = await client.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = aracId,
                category = "Otopark",
                date = DateTime.UtcNow.Date.AddMonths(4),
                amount = 500m
            });

            Assert.Contains("gelecek", (await MesajAl(cevap)).ToLowerInvariant());
        }

        [Fact]
        public async Task SinirDisiTutarAcikMesajDoner()
        {
            var (client, aracId) = await HazirlaAsync("hatatutar", "34HB1002");

            var cevap = await client.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = aracId,
                category = "Otopark",
                date = DateTime.UtcNow.Date.AddDays(-1),
                amount = 9_000_000m
            });

            Assert.Contains("tutar", (await MesajAl(cevap)).ToLowerInvariant());
        }

        [Fact]
        public async Task KisaSifreAcikMesajDoner()
        {
            var client = _factory.CreateClient();

            var cevap = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("hatasifre"), fullName = "Hata", password = "abc" });

            Assert.Contains("şifre", (await MesajAl(cevap)).ToLowerInvariant());
        }
    }
}
