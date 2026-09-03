using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class PlakaKuraliHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public PlakaKuraliHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Plaka Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static object Arac(string plaka, bool yabanci = false) => new
        {
            plate = plaka,
            yabanciPlaka = yabanci,
            brand = "Fiat",
            model = "Egea",
            year = 2020,
            currentKm = 40000,
            fuelType = "Dizel",
            vites = "Manuel",
            kasaTipi = "Sedan"
        };

        private static int Ek() => Random.Shared.Next(10, 99);

        [Fact]
        public async Task BosluklarVeKucukHarfNormalizeEdilir()
        {
            var client = await SahipAsync("normalize");
            var govde = "34 abc " + Ek();

            var cevap = await client.PostAsJsonAsync("/api/Vehicles", Arac(govde));
            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());

            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            var plaka = belge.RootElement.GetProperty("data").GetProperty("plate").GetString();

            Assert.Equal(govde.Replace(" ", string.Empty).ToUpperInvariant(), plaka);
        }

        [Theory]
        [InlineData("82ABC123")]
        [InlineData("34ABCD12")]
        [InlineData("34A123")]
        [InlineData("34ÇBC123")]
        public async Task KuralDisiPlakaReddedilir(string plaka)
        {
            var client = await SahipAsync("kuraldisi");

            var cevap = await client.PostAsJsonAsync("/api/Vehicles", Arac(plaka));

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task YabanciPlakaIsaretlenirseSerbestKabulEdilir()
        {
            var client = await SahipAsync("yabanci");

            var cevap = await client.PostAsJsonAsync("/api/Vehicles", Arac("BMW" + Ek() + Ek(), true));

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());

            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            Assert.True(belge.RootElement.GetProperty("data").GetProperty("yabanciPlaka").GetBoolean());
        }

        [Fact]
        public async Task YabanciBayragiOlmadanAyniPlakaReddedilir()
        {
            var client = await SahipAsync("yabancisiz");

            var cevap = await client.PostAsJsonAsync("/api/Vehicles", Arac("BMW1234"));

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }
    }
}
