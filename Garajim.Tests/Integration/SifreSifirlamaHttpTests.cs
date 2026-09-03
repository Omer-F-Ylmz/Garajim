using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class SifreSifirlamaHttpTests : IDisposable
    {
        private readonly GarajimWebApplicationFactory _factory = new GarajimWebApplicationFactory();

        public void Dispose() => _factory.Dispose();

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, string Eposta)> KullaniciAsync(string on)
        {
            var client = _factory.CreateClient();
            var eposta = Eposta(on);
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "Şifre Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, eposta);
        }

        private static async Task<string> MesajAsync(HttpResponseMessage cevap)
        {
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            return belge.RootElement.GetProperty("message").GetString();
        }

        private async Task<string> KodIsteAsync(HttpClient client, string eposta)
        {
            var cevap = await client.PostAsJsonAsync("/api/Auth/sifre-sifirla-kod", new { email = eposta });
            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            return SahteEpostaGonderici.Ortak.SonKod(eposta);
        }

        private void KullaniciyiDuzenle(string eposta, Action<AppUser> islem)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();
            var user = context.Users.IgnoreQueryFilters().Single(u => u.Email == eposta);
            islem(user);
            context.SaveChanges();
        }

        [Fact]
        public async Task VarOlanVeOlmayanEpostaAyniYanitiAlir()
        {
            var (_, eposta) = await KullaniciAsync("sifirlama");
            var anonim = _factory.CreateClient();

            var varOlan = await anonim.PostAsJsonAsync("/api/Auth/sifre-sifirla-kod", new { email = eposta });
            var olmayan = await anonim.PostAsJsonAsync("/api/Auth/sifre-sifirla-kod", new { email = Eposta("yok") });

            Assert.Equal(HttpStatusCode.OK, varOlan.StatusCode);
            Assert.Equal(HttpStatusCode.OK, olmayan.StatusCode);
            Assert.Equal(await MesajAsync(varOlan), await MesajAsync(olmayan));
        }

        [Fact]
        public async Task EpostaKonusuSifreSifirlamaOlarakGelir()
        {
            var (_, eposta) = await KullaniciAsync("konu");
            var anonim = _factory.CreateClient();

            await anonim.PostAsJsonAsync("/api/Auth/sifre-sifirla-kod", new { email = eposta });

            var gonderilen = SahteEpostaGonderici.Ortak.Gonderilenler
                .Last(g => string.Equals(g.Alici, eposta, StringComparison.OrdinalIgnoreCase));

            Assert.Equal("Garajım şifre sıfırlama kodunuz", gonderilen.Konu);
        }

        [Fact]
        public async Task DogruKodSifreyiDegistirirVeTokenDonmez()
        {
            var (_, eposta) = await KullaniciAsync("degistir");
            var anonim = _factory.CreateClient();
            var kod = await KodIsteAsync(anonim, eposta);

            var sifirla = await anonim.PostAsJsonAsync("/api/Auth/sifre-sifirla",
                new { email = eposta, kod, yeniSifre = "YeniSifre1!" });

            Assert.Equal(HttpStatusCode.OK, sifirla.StatusCode);

            using (var belge = JsonDocument.Parse(await sifirla.Content.ReadAsStringAsync()))
            {
                var tokenVar = belge.RootElement.TryGetProperty("data", out var veri)
                    && veri.ValueKind == JsonValueKind.Object
                    && veri.TryGetProperty("token", out _);
                Assert.False(tokenVar);
            }

            var eskiSifre = await anonim.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = "Test1234!" });
            Assert.Equal(HttpStatusCode.Unauthorized, eskiSifre.StatusCode);

            var yeniSifre = await anonim.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = "YeniSifre1!" });
            Assert.Equal(HttpStatusCode.OK, yeniSifre.StatusCode);
        }

        [Fact]
        public async Task KodTekKullanimliktir()
        {
            var (_, eposta) = await KullaniciAsync("tekkullanim");
            var anonim = _factory.CreateClient();
            var kod = await KodIsteAsync(anonim, eposta);

            var birinci = await anonim.PostAsJsonAsync("/api/Auth/sifre-sifirla",
                new { email = eposta, kod, yeniSifre = "YeniSifre1!" });
            var ikinci = await anonim.PostAsJsonAsync("/api/Auth/sifre-sifirla",
                new { email = eposta, kod, yeniSifre = "BaskaSifre1!" });

            Assert.Equal(HttpStatusCode.OK, birinci.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);
        }

        [Fact]
        public async Task BesYanlisDenemeKoduYakar()
        {
            var (_, eposta) = await KullaniciAsync("deneme");
            var anonim = _factory.CreateClient();
            var kod = await KodIsteAsync(anonim, eposta);

            for (var i = 0; i < 5; i++)
            {
                var yanlis = await anonim.PostAsJsonAsync("/api/Auth/sifre-sifirla",
                    new { email = eposta, kod = "000000", yeniSifre = "YeniSifre1!" });
                Assert.Equal(HttpStatusCode.BadRequest, yanlis.StatusCode);
            }

            var dogru = await anonim.PostAsJsonAsync("/api/Auth/sifre-sifirla",
                new { email = eposta, kod, yeniSifre = "YeniSifre1!" });

            Assert.Equal(HttpStatusCode.BadRequest, dogru.StatusCode);
        }

        [Fact]
        public async Task SuresiDolmusKodReddedilir()
        {
            var (_, eposta) = await KullaniciAsync("sure");
            var anonim = _factory.CreateClient();
            var kod = await KodIsteAsync(anonim, eposta);

            KullaniciyiDuzenle(eposta, u => u.SifirlamaKodSonTarih = DateTime.UtcNow.AddMinutes(-1));

            var cevap = await anonim.PostAsJsonAsync("/api/Auth/sifre-sifirla",
                new { email = eposta, kod, yeniSifre = "YeniSifre1!" });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task AltmisSaniyeIcindeIkinciKodGonderilmez()
        {
            var (_, eposta) = await KullaniciAsync("aralik");
            var anonim = _factory.CreateClient();

            await KodIsteAsync(anonim, eposta);
            var ilkSayi = SahteEpostaGonderici.Ortak.SayiOf(eposta);

            await anonim.PostAsJsonAsync("/api/Auth/sifre-sifirla-kod", new { email = eposta });

            Assert.Equal(ilkSayi, SahteEpostaGonderici.Ortak.SayiOf(eposta));
        }

        [Fact]
        public async Task ZayifYeniSifreReddedilir()
        {
            var (_, eposta) = await KullaniciAsync("zayif");
            var anonim = _factory.CreateClient();
            var kod = await KodIsteAsync(anonim, eposta);

            var cevap = await anonim.PostAsJsonAsync("/api/Auth/sifre-sifirla",
                new { email = eposta, kod, yeniSifre = "12345" });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task SifirlamaSonrasiEskiTokenReddedilir()
        {
            var (client, eposta) = await KullaniciAsync("eskitoken");

            var once = await client.GetAsync("/api/Vehicles");
            Assert.Equal(HttpStatusCode.OK, once.StatusCode);

            await Task.Delay(1100);

            var anonim = _factory.CreateClient();
            var kod = await KodIsteAsync(anonim, eposta);
            var sifirla = await anonim.PostAsJsonAsync("/api/Auth/sifre-sifirla",
                new { email = eposta, kod, yeniSifre = "YeniSifre1!" });
            Assert.Equal(HttpStatusCode.OK, sifirla.StatusCode);

            var sonra = await client.GetAsync("/api/Vehicles");

            Assert.Equal(HttpStatusCode.Unauthorized, sonra.StatusCode);
        }
    }
}
