using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class PlanLimitHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public PlanLimitHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("plan"), fullName = "Plan Sahip", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static Task<HttpResponseMessage> AracEkleAsync(HttpClient client, string plaka)
        {
            return client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 100000,
                fuelType = "Benzin"
            });
        }

        [Fact]
        public async Task BireyselPlanUcuncuAractanSonra402Doner()
        {
            var sahip = await SahipOlusturAsync();
            var ek = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();

            for (var i = 1; i <= 3; i++)
            {
                var cevap = await AracEkleAsync(sahip, $"34{ek}{i}");
                Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            }

            var dorduncu = await AracEkleAsync(sahip, $"34{ek}4");

            Assert.Equal(HttpStatusCode.PaymentRequired, dorduncu.StatusCode);
            var govde = JsonDocument.Parse(await dorduncu.Content.ReadAsStringAsync()).RootElement;
            Assert.Contains("limit", govde.GetProperty("message").GetString());
        }

        [Fact]
        public async Task LimitSirketBazindaSayilir()
        {
            var birinci = await SahipOlusturAsync();
            var ekA = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();
            for (var i = 1; i <= 3; i++)
            {
                await AracEkleAsync(birinci, $"06{ekA}{i}");
            }

            var ikinci = await SahipOlusturAsync();
            var ekB = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();
            var cevap = await AracEkleAsync(ikinci, $"06{ekB}1");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        }

        [Fact]
        public async Task SilinenAracLimitiSerbestBirakir()
        {
            var sahip = await SahipOlusturAsync();
            var ek = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();

            var ilkCevap = await AracEkleAsync(sahip, $"35{ek}1");
            var ilkId = JsonDocument.Parse(await ilkCevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
            await AracEkleAsync(sahip, $"35{ek}2");
            await AracEkleAsync(sahip, $"35{ek}3");

            Assert.Equal(HttpStatusCode.PaymentRequired, (await AracEkleAsync(sahip, $"35{ek}4")).StatusCode);

            await sahip.DeleteAsync($"/api/Vehicles/{ilkId}");

            Assert.Equal(HttpStatusCode.OK, (await AracEkleAsync(sahip, $"35{ek}4")).StatusCode);
        }
    }
}
