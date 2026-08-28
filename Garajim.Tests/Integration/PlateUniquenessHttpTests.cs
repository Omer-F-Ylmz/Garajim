using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class PlateUniquenessHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public PlateUniquenessHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "Filo Sahibi", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<HttpClient> EkipUyesiOlusturAsync(HttpClient sahip, string rol)
        {
            var eposta = Eposta("uye");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Ekip Üyesi", role = rol });
            var sifre = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("temporaryPassword").GetString();

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = sifre });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static Task<HttpResponseMessage> AracEkleAsync(HttpClient client, string plaka)
        {
            return client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Ford",
                model = "Transit",
                year = 2020,
                currentKm = 50000,
                fuelType = "Dizel"
            });
        }

        [Fact]
        public async Task AyniSirkettekiBaskaKullaniciAyniPlakayiEkleyemez()
        {
            var sahip = await SahipOlusturAsync();
            var yonetici = await EkipUyesiOlusturAsync(sahip, "Manager");

            var ilk = await AracEkleAsync(sahip, "34PLK001");
            Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);

            var ikinci = await AracEkleAsync(yonetici, "34PLK001");
            var govde = await ikinci.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);
            Assert.Contains("plaka", govde, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task FarkliSirketlerAyniPlakayiKullanabilir()
        {
            var birinciSirket = await SahipOlusturAsync();
            var ikinciSirket = await SahipOlusturAsync();

            var ilk = await AracEkleAsync(birinciSirket, "06PLK002");
            var ikinci = await AracEkleAsync(ikinciSirket, "06PLK002");

            Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);
            Assert.Equal(HttpStatusCode.OK, ikinci.StatusCode);
        }

        [Fact]
        public async Task AyniKullanicininTekrarEklediginPlakaReddedilir()
        {
            var sahip = await SahipOlusturAsync();

            await AracEkleAsync(sahip, "35PLK003");
            var ikinci = await AracEkleAsync(sahip, "35 plk 003");

            Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);
        }
    }
}
