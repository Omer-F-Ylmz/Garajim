using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Business.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Garajim.Dal.Concrete.Context;

namespace Garajim.Tests.Integration
{
    public class DogrulamaFactory : GarajimWebApplicationFactory
    {
        public SahteEpostaGonderici Gonderici { get; } = new SahteEpostaGonderici();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                foreach (var kayit in services.Where(d => d.ServiceType == typeof(IEmailSender)).ToList())
                {
                    services.Remove(kayit);
                }

                services.AddSingleton<IEmailSender>(Gonderici);
            });
        }
    }

    public class EmailDogrulamaHttpTests : IDisposable
    {
        private readonly DogrulamaFactory _factory = new DogrulamaFactory();

        public void Dispose()
        {
            _factory.Dispose();
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private Task<HttpResponseMessage> KayitAsync(HttpClient client, string eposta)
        {
            return client.PostAsJsonAsync("/api/Auth/register", new { email = eposta, fullName = "Doğrulama Kullanıcı", password = "Test1234!" });
        }

        private static async Task<JsonElement> GovdeAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement;
        }

        private async Task<T> VeritabaniAsync<T>(Func<GarajimDbContext, Task<T>> is_)
        {
            using var kapsam = _factory.Services.CreateScope();
            return await is_(kapsam.ServiceProvider.GetRequiredService<GarajimDbContext>());
        }

        [Fact]
        public async Task KayitTokenDonmezDogrulamaGerekliDoner()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("kayit");

            var cevap = await KayitAsync(client, eposta);
            var govde = await GovdeAsync(cevap);

            Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);
            Assert.True(govde.GetProperty("data").GetProperty("dogrulamaGerekli").GetBoolean());
            Assert.False(govde.GetProperty("data").TryGetProperty("token", out var token) && token.ValueKind == JsonValueKind.String,
                "Kayıt yanıtında JWT dönmemeli.");
            Assert.DoesNotContain("eyJ", await cevap.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task KayitSonrasiTurkceKodEpostasiGider()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("epostametin");

            await KayitAsync(client, eposta);

            var mesaj = _factory.Gonderici.Gonderilenler.Single(g => g.Alici == eposta);

            Assert.Contains("Garajım doğrulama kodunuz", mesaj.Govde);
            Assert.Contains("siz istemediyseniz", mesaj.Govde, StringComparison.OrdinalIgnoreCase);
            Assert.Matches(@"\b\d{6}\b", mesaj.Govde);
        }

        [Fact]
        public async Task DogruKodTokenDonerVeKodSilinir()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("dogrukod");
            await KayitAsync(client, eposta);

            var kod = _factory.Gonderici.SonKod(eposta);
            var cevap = await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod });
            var govde = await GovdeAsync(cevap);

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(govde.GetProperty("data").GetProperty("token").GetString()));

            var kullanici = await VeritabaniAsync(db => db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == eposta));
            Assert.True(kullanici.EmailDogrulandi);
            Assert.Null(kullanici.DogrulamaKodHash);
            Assert.Null(kullanici.DogrulamaKodSonTarih);
        }

        [Fact]
        public async Task KodTekKullanimlik()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("tekkullanim");
            await KayitAsync(client, eposta);
            var kod = _factory.Gonderici.SonKod(eposta);

            var ilk = await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod });
            var ikinci = await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod });

            Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);
        }

        [Fact]
        public async Task BesYanlisDenemeKoduIptalEder()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("yanlisdeneme");
            await KayitAsync(client, eposta);
            var kod = _factory.Gonderici.SonKod(eposta);

            for (var i = 0; i < 5; i++)
            {
                var yanlis = await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod = "000000" });
                Assert.Equal(HttpStatusCode.BadRequest, yanlis.StatusCode);
            }

            var dogruAmaIptal = await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod });
            Assert.Equal(HttpStatusCode.BadRequest, dogruAmaIptal.StatusCode);

            var kullanici = await VeritabaniAsync(db => db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == eposta));
            Assert.False(kullanici.EmailDogrulandi);
            Assert.Null(kullanici.DogrulamaKodHash);
        }

        [Fact]
        public async Task SuresiDolmusKodReddedilir()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("suredolu");
            await KayitAsync(client, eposta);
            var kod = _factory.Gonderici.SonKod(eposta);

            using (var kapsam = _factory.Services.CreateScope())
            {
                var db = kapsam.ServiceProvider.GetRequiredService<GarajimDbContext>();
                var kullanici = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == eposta);
                kullanici.DogrulamaKodSonTarih = DateTime.UtcNow.AddMinutes(-1);
                await db.SaveChangesAsync();
            }

            var cevap = await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task KodVeritabaninaDuzMetinYazilmaz()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("hashli");
            await KayitAsync(client, eposta);
            var kod = _factory.Gonderici.SonKod(eposta);

            var kullanici = await VeritabaniAsync(db => db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == eposta));

            Assert.False(string.IsNullOrWhiteSpace(kullanici.DogrulamaKodHash));
            Assert.NotEqual(kod, kullanici.DogrulamaKodHash);
            Assert.DoesNotContain(kod, kullanici.DogrulamaKodHash);
            Assert.Equal(64, kullanici.DogrulamaKodHash.Length);
        }

        [Fact]
        public async Task DogrulanmamisGiris403VeKodDoner()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("girisyok");
            await KayitAsync(client, eposta);

            var cevap = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = "Test1234!" });
            var govde = await GovdeAsync(cevap);

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
            Assert.Equal("EMAIL_DOGRULANMADI", govde.GetProperty("kod").GetString());
            Assert.DoesNotContain("eyJ", await cevap.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task DogrulandiktanSonraGirisCalisir()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("girisvar");
            await KayitAsync(client, eposta);
            await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod = _factory.Gonderici.SonKod(eposta) });

            var cevap = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = "Test1234!" });
            var govde = await GovdeAsync(cevap);

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(govde.GetProperty("data").GetProperty("token").GetString()));
        }

        [Fact]
        public async Task KodGonderVarOlmayanVeVarOlanIcinAyniYanit()
        {
            var client = _factory.CreateClient();
            var kayitli = Eposta("varolan");
            await KayitAsync(client, kayitli);

            using (var kapsam = _factory.Services.CreateScope())
            {
                var db = kapsam.ServiceProvider.GetRequiredService<GarajimDbContext>();
                var kullanici = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == kayitli);
                kullanici.SonKodGonderim = DateTime.UtcNow.AddMinutes(-5);
                await db.SaveChangesAsync();
            }

            var varOlan = await client.PostAsJsonAsync("/api/Auth/kod-gonder", new { email = kayitli });
            var yok = await client.PostAsJsonAsync("/api/Auth/kod-gonder", new { email = Eposta("hicyok") });

            Assert.Equal(HttpStatusCode.OK, varOlan.StatusCode);
            Assert.Equal(HttpStatusCode.OK, yok.StatusCode);
            Assert.Equal(await varOlan.Content.ReadAsStringAsync(), await yok.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task KodGonderAltmisSaniyeIcindeTekrarlanamaz()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("araliksiz");
            await KayitAsync(client, eposta);

            var oncekiSayi = _factory.Gonderici.SayiOf(eposta);
            var cevap = await client.PostAsJsonAsync("/api/Auth/kod-gonder", new { email = eposta });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal(oncekiSayi, _factory.Gonderici.SayiOf(eposta));
        }

        [Fact]
        public async Task KodGonderSaatteEnFazlaBesKez()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("saatlik");
            await KayitAsync(client, eposta);

            for (var i = 0; i < 6; i++)
            {
                using var kapsam = _factory.Services.CreateScope();
                var db = kapsam.ServiceProvider.GetRequiredService<GarajimDbContext>();
                var kullanici = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == eposta);
                kullanici.SonKodGonderim = DateTime.UtcNow.AddMinutes(-2);
                await db.SaveChangesAsync();

                await client.PostAsJsonAsync("/api/Auth/kod-gonder", new { email = eposta });
            }

            Assert.True(_factory.Gonderici.SayiOf(eposta) <= 6,
                "Kayıt e-postası dahil saatte en fazla 6 gönderim olmalı, oldu: " + _factory.Gonderici.SayiOf(eposta));
        }

        [Fact]
        public async Task YenidenGonderilenKodEskisiniGecersizKilar()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("yenikod");
            await KayitAsync(client, eposta);
            var eskiKod = _factory.Gonderici.SonKod(eposta);

            using (var kapsam = _factory.Services.CreateScope())
            {
                var db = kapsam.ServiceProvider.GetRequiredService<GarajimDbContext>();
                var kullanici = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == eposta);
                kullanici.SonKodGonderim = DateTime.UtcNow.AddMinutes(-5);
                await db.SaveChangesAsync();
            }

            await client.PostAsJsonAsync("/api/Auth/kod-gonder", new { email = eposta });
            var yeniKod = _factory.Gonderici.SonKod(eposta);

            Assert.NotEqual(eskiKod, yeniKod);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod = eskiKod })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod = yeniKod })).StatusCode);
        }
    }
}
