using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class UrunTuruHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public UrunTuruHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Tur", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<JsonElement> DurumAsync(HttpClient client)
        {
            var cevap = await client.GetAsync("/api/Kurulum");
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task YeniHesaptaTurTamamlanmamis()
        {
            var client = await SahipAsync("turyeni");

            Assert.False((await DurumAsync(client)).GetProperty("turTamamlandi").GetBoolean());
        }

        [Fact]
        public async Task TurTamamIsaretlenirVeKalicidir()
        {
            var client = await SahipAsync("turtamam");

            var cevap = await client.PostAsync("/api/Kurulum/tur-tamam", null);
            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            Assert.True((await DurumAsync(client)).GetProperty("turTamamlandi").GetBoolean());
        }
    }
}
