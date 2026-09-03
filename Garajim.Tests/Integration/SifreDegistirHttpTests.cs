using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class SifreDegistirHttpTests : IDisposable
    {
        private readonly GarajimWebApplicationFactory _factory = new GarajimWebApplicationFactory();

        public void Dispose() => _factory.Dispose();

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, string Eposta)> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var eposta = Eposta(on);
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "Şifre Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, eposta);
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            return belge.RootElement.GetProperty("data");
        }

        [Fact]
        public async Task KimliksizIstekReddedilir()
        {
            var anonim = _factory.CreateClient();

            var cevap = await anonim.PostAsJsonAsync("/api/Auth/sifre-degistir",
                new { mevcut = "Test1234!", yeni = "YeniSifre1!" });

            Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
        }

        [Fact]
        public async Task MevcutSifreYanlissaDortYuz()
        {
            var (client, _) = await SahipAsync("yanlis");

            var cevap = await client.PostAsJsonAsync("/api/Auth/sifre-degistir",
                new { mevcut = "HataliSifre1!", yeni = "YeniSifre1!" });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task ZayifYeniSifreReddedilir()
        {
            var (client, _) = await SahipAsync("zayif");

            var cevap = await client.PostAsJsonAsync("/api/Auth/sifre-degistir",
                new { mevcut = "Test1234!", yeni = "123" });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task DegisimSonrasiDigerOturumlarDuserVeYeniSifreCalisir()
        {
            var (client, eposta) = await SahipAsync("degisim");

            var digerOturum = _factory.CreateClient();
            var giris = await digerOturum.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = "Test1234!" });
            var digerToken = (await VeriAsync(giris)).GetProperty("token").GetString();
            digerOturum.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", digerToken);
            Assert.Equal(HttpStatusCode.OK, (await digerOturum.GetAsync("/api/Vehicles")).StatusCode);

            await Task.Delay(1100);

            var degistir = await client.PostAsJsonAsync("/api/Auth/sifre-degistir",
                new { mevcut = "Test1234!", yeni = "YeniSifre1!" });
            Assert.Equal(HttpStatusCode.OK, degistir.StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized, (await digerOturum.GetAsync("/api/Vehicles")).StatusCode);

            var anonim = _factory.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await anonim.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = "Test1234!" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK,
                (await anonim.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = "YeniSifre1!" })).StatusCode);
        }

        [Fact]
        public async Task DavetliSurucuGeciciSifreBayragiTasirVeDegisimdeDuser()
        {
            var (sahip, _) = await SahipAsync("ekip");
            var surucuEposta = Eposta("surucu");

            var ekle = await sahip.PostAsJsonAsync("/api/Team",
                new { email = surucuEposta, fullName = "Geçici Sürücü", role = "Driver" });
            var geciciSifre = (await VeriAsync(ekle)).GetProperty("temporaryPassword").GetString();

            var surucu = _factory.CreateClient();
            var giris = await surucu.PostAsJsonAsync("/api/Auth/login", new { email = surucuEposta, password = geciciSifre });
            var girisVerisi = await VeriAsync(giris);

            Assert.True(girisVerisi.GetProperty("geciciSifre").GetBoolean());

            surucu.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", girisVerisi.GetProperty("token").GetString());

            var degistir = await surucu.PostAsJsonAsync("/api/Auth/sifre-degistir",
                new { mevcut = geciciSifre, yeni = "SurucuSifre1!" });
            Assert.Equal(HttpStatusCode.OK, degistir.StatusCode);

            var anonim = _factory.CreateClient();
            var yeniGiris = await anonim.PostAsJsonAsync("/api/Auth/login",
                new { email = surucuEposta, password = "SurucuSifre1!" });

            Assert.False((await VeriAsync(yeniGiris)).GetProperty("geciciSifre").GetBoolean());
        }

        [Fact]
        public async Task KendiKayitOlanKullaniciGeciciSifreBayragiTasimaz()
        {
            var (_, eposta) = await SahipAsync("kendi");

            var anonim = _factory.CreateClient();
            var giris = await anonim.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = "Test1234!" });

            Assert.False((await VeriAsync(giris)).GetProperty("geciciSifre").GetBoolean());
        }
    }
}
