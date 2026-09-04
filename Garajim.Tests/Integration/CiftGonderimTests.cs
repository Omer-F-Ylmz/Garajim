using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class CiftGonderimTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public CiftGonderimTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int AracId)> HazirlaAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Çift Gönderim", password = "Test1234!" });
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

        private static async Task<int> SayAsync(HttpClient client, string yol)
        {
            var cevap = await client.GetAsync(yol);
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetArrayLength();
        }

        [Fact]
        public async Task UcHizliMasrafTekKayitOlusturur()
        {
            var (client, aracId) = await HazirlaAsync("masraf");
            var govde = new { vehicleId = aracId, category = "Otopark", date = "2026-08-02", amount = 123m, note = "aynı" };

            var cevaplar = new List<HttpResponseMessage>();
            for (var i = 0; i < 3; i++)
            {
                cevaplar.Add(await client.PostAsJsonAsync("/api/Expenses", govde));
            }

            Assert.All(cevaplar, c => Assert.Equal(HttpStatusCode.OK, c.StatusCode));
            Assert.Equal(1, await SayAsync(client, $"/api/Expenses?vehicleId={aracId}"));
        }

        [Fact]
        public async Task UcHizliBakimTekKayitOlusturur()
        {
            var (client, aracId) = await HazirlaAsync("bakim");
            var govde = new
            {
                vehicleId = aracId, type = "PeriyodikBakim", date = "2026-08-02",
                km = 10500, cost = 1000m, serviceName = "Servis", note = "aynı"
            };

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Maintenance", govde)).StatusCode);
            }

            Assert.Equal(1, await SayAsync(client, $"/api/Maintenance?vehicleId={aracId}"));
        }

        [Fact]
        public async Task FarkliGovdeAyriKayitOlusturur()
        {
            var (client, aracId) = await HazirlaAsync("farkli");

            await client.PostAsJsonAsync("/api/Expenses", new { vehicleId = aracId, category = "Otopark", date = "2026-08-02", amount = 100m, note = "bir" });
            await client.PostAsJsonAsync("/api/Expenses", new { vehicleId = aracId, category = "Otopark", date = "2026-08-02", amount = 200m, note = "iki" });

            Assert.Equal(2, await SayAsync(client, $"/api/Expenses?vehicleId={aracId}"));
        }
    }
}
