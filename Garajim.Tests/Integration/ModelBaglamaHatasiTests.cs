using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class ModelBaglamaHatasiTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public ModelBaglamaHatasiTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Bağlama", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static StringContent Govde(string json) => new StringContent(json, Encoding.UTF8, "application/json");

        [Theory]
        [InlineData("{\"vehicleId\":1,\"category\":\"Otopark\",\"date\":\"2026-02-29\",\"amount\":100}")]
        [InlineData("{\"vehicleId\":1,\"category\":\"Otopark\",\"date\":\"abc\",\"amount\":100}")]
        [InlineData("{\"vehicleId\":1,\"category\":\"Otopark\",\"date\":\"2026-08-01\",\"amount\":\"yuz\"}")]
        [InlineData("{\"vehicleId\":\"birseyler\",\"category\":\"Otopark\",\"date\":\"2026-08-01\",\"amount\":100}")]
        public async Task BaglamaHatasiTurkceZarfDoner(string json)
        {
            var client = await SahipAsync("baglama");

            var cevap = await client.PostAsync("/api/Expenses", Govde(json));
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
            Assert.DoesNotContain("traceId", govde, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("$.", govde, StringComparison.Ordinal);
            Assert.DoesNotContain("System.", govde, StringComparison.Ordinal);

            using var belge = JsonDocument.Parse(govde);
            Assert.False(belge.RootElement.GetProperty("success").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(belge.RootElement.GetProperty("message").GetString()));
        }

        [Fact]
        public async Task GecersizTarihMesajiTarihiIsaretEder()
        {
            var client = await SahipAsync("tarih");

            var cevap = await client.PostAsync("/api/Expenses",
                Govde("{\"vehicleId\":1,\"category\":\"Otopark\",\"date\":\"2026-02-29\",\"amount\":100}"));

            var mesaj = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync())
                .RootElement.GetProperty("message").GetString();

            Assert.Contains("tarih", mesaj, StringComparison.OrdinalIgnoreCase);
        }
    }
}
