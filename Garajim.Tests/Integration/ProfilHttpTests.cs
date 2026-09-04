using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class ProfilHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public ProfilHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, string Eposta)> SahipAsync(string on)
        {
            var eposta = Eposta(on);
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "Profil Kullanıcı", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, eposta);
        }

        private static async Task<JsonElement> ProfilAsync(HttpClient client)
        {
            var cevap = await client.GetAsync("/api/Account/profil");
            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task ProfilAdVeTercihleriDoner()
        {
            var (client, eposta) = await SahipAsync("profiloku");

            var veri = await ProfilAsync(client);

            Assert.Equal("Profil Kullanıcı", veri.GetProperty("fullName").GetString());
            Assert.Equal(eposta, veri.GetProperty("email").GetString());
            Assert.True(veri.GetProperty("bildirimEvrak").GetBoolean());
            Assert.True(veri.GetProperty("bildirimHatirlatma").GetBoolean());
        }

        [Fact]
        public async Task AdVeTercihlerGuncellenir()
        {
            var (client, _) = await SahipAsync("profilyaz");

            var cevap = await client.PutAsJsonAsync("/api/Account/profil", new
            {
                fullName = "Yeni Ad Soyad",
                bildirimEvrak = false,
                bildirimHatirlatma = true
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var veri = await ProfilAsync(client);

            Assert.Equal("Yeni Ad Soyad", veri.GetProperty("fullName").GetString());
            Assert.False(veri.GetProperty("bildirimEvrak").GetBoolean());
            Assert.True(veri.GetProperty("bildirimHatirlatma").GetBoolean());
        }

        [Fact]
        public async Task BosAdReddedilir()
        {
            var (client, _) = await SahipAsync("profilbosad");

            var cevap = await client.PutAsJsonAsync("/api/Account/profil", new
            {
                fullName = "  ",
                bildirimEvrak = true,
                bildirimHatirlatma = true
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task EpostaDegisimiKodlaTamamlanir()
        {
            var (client, eski) = await SahipAsync("epostadegis");
            var yeni = Eposta("epostayeni");

            var kodCevabi = await client.PostAsJsonAsync("/api/Account/eposta-degistir-kod", new { yeniEposta = yeni });
            Assert.Equal(HttpStatusCode.OK, kodCevabi.StatusCode);

            var kod = SahteEpostaGonderici.Ortak.SonKod(yeni);
            Assert.False(string.IsNullOrEmpty(kod), "Kod yeni adrese gitmeli.");
            Assert.True(SahteEpostaGonderici.Ortak.SayiOf(eski) > 0, "Eski adrese bilgi maili gitmeli.");

            var degistir = await client.PostAsJsonAsync("/api/Account/eposta-degistir", new { kod });
            Assert.Equal(HttpStatusCode.OK, degistir.StatusCode);

            var veri = await ProfilAsync(client);
            Assert.Equal(yeni, veri.GetProperty("email").GetString());
        }

        [Fact]
        public async Task YanlisKodReddedilir()
        {
            var (client, _) = await SahipAsync("epostayanlis");
            var yeni = Eposta("epostayanlisyeni");

            await client.PostAsJsonAsync("/api/Account/eposta-degistir-kod", new { yeniEposta = yeni });

            var cevap = await client.PostAsJsonAsync("/api/Account/eposta-degistir", new { kod = "000000" });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task KodIstenmedenDegistirmeReddedilir()
        {
            var (client, _) = await SahipAsync("epostakodsuz");

            var cevap = await client.PostAsJsonAsync("/api/Account/eposta-degistir", new { kod = "123456" });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task KullanilanEpostaCakismasi409Doner()
        {
            var (birinci, birinciEposta) = await SahipAsync("epostacakisma1");
            var (ikinci, _) = await SahipAsync("epostacakisma2");

            var cevap = await ikinci.PostAsJsonAsync("/api/Account/eposta-degistir-kod", new { yeniEposta = birinciEposta });

            Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
            Assert.NotNull(birinci);
        }

        [Fact]
        public async Task GecersizEpostaReddedilir()
        {
            var (client, _) = await SahipAsync("epostagecersiz");

            var cevap = await client.PostAsJsonAsync("/api/Account/eposta-degistir-kod", new { yeniEposta = "bu-bir-eposta-degil" });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }
    }
}
