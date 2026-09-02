using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Business.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Garajim.Dal.Concrete.Context;

namespace Garajim.Tests.Integration
{
    public class EmailDogrulamaUctanUcaTests : IDisposable
    {
        private readonly GarajimWebApplicationFactory _factory = new GarajimWebApplicationFactory();

        public void Dispose()
        {
            _factory.Dispose();
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        [Fact]
        public async Task KayitBastanSonaCalisirVeAracEklenebilir()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("uctanuca");

            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "Uçtan Uca", password = "Test1234!" });

            Assert.Equal(HttpStatusCode.Created, kayit.StatusCode);

            var kod = SahteEpostaGonderici.Ortak.SonKod(eposta);
            Assert.Matches(@"^\d{6}$", kod);

            var korumaliUcOnce = await client.GetAsync("/api/Vehicles");
            Assert.Equal(HttpStatusCode.Unauthorized, korumaliUcOnce.StatusCode);

            var dogrula = await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod });
            var token = JsonDocument.Parse(await dogrula.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("token").GetString();

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = "34UU" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant(),
                brand = "Renault",
                model = "Clio",
                year = 2020,
                currentKm = 50000,
                fuelType = "Benzin",
                vites = "Otomatik",
                kasaTipi = "Hatchback5"
            });

            Assert.Equal(HttpStatusCode.OK, arac.StatusCode);
        }

        [Fact]
        public async Task SahteGondericiKoduYakalarVeMetinTurkce()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("gonderici");

            await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "Gönderici Testi", password = "Test1234!" });

            var mesaj = SahteEpostaGonderici.Ortak.Gonderilenler.Single(g => g.Alici == eposta);

            Assert.Equal(DogrulamaKodu.EpostaKonusu, mesaj.Konu);
            Assert.Contains("Garajım doğrulama kodunuz:", mesaj.Govde);
            Assert.Contains("10 dakika", mesaj.Govde);
            Assert.Contains("siz istemediyseniz", mesaj.Govde);
            Assert.Matches(@"\b\d{6}\b", mesaj.Govde);
        }

        [Fact]
        public async Task UcUctaAuthRateLimitPolitikasiTasiniyor()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);

            var kaynak = await File.ReadAllTextAsync(
                Path.Combine(kok.FullName, "Garajim.API", "Controllers", "AuthController.cs"));

            Assert.Contains("[EnableRateLimiting(AuthController.RateLimitPolicy)]", kaynak);
            Assert.Contains("[HttpPost(\"dogrula\")]", kaynak);
            Assert.Contains("[HttpPost(\"kod-gonder\")]", kaynak);

            var sinifBaslangici = kaynak.IndexOf("public class AuthController", StringComparison.Ordinal);
            var politika = kaynak.IndexOf("[EnableRateLimiting(AuthController.RateLimitPolicy)]", StringComparison.Ordinal);

            Assert.True(politika < sinifBaslangici, "Politika sınıf düzeyinde olmalı ki üç uç da devralsın.");
        }

        [Fact]
        public async Task DogrulanmisKullaniciyaKodGondermekYeniKodUretmez()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("zatendogru");

            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "Zaten Doğru", password = "Test1234!" });
            await TestKayit.TokenAl(client, kayit);

            var oncekiSayi = SahteEpostaGonderici.Ortak.SayiOf(eposta);

            using (var kapsam = _factory.Services.CreateScope())
            {
                var db = kapsam.ServiceProvider.GetRequiredService<GarajimDbContext>();
                var kullanici = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == eposta);
                kullanici.SonKodGonderim = DateTime.UtcNow.AddMinutes(-5);
                await db.SaveChangesAsync();
            }

            var cevap = await client.PostAsJsonAsync("/api/Auth/kod-gonder", new { email = eposta });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal(oncekiSayi, SahteEpostaGonderici.Ortak.SayiOf(eposta));

            var kullaniciSon = await KullaniciAsync(eposta);
            Assert.True(kullaniciSon.EmailDogrulandi);
            Assert.Null(kullaniciSon.DogrulamaKodHash);
        }

        [Fact]
        public async Task DogrulamaSonrasiDenemeSayaciSifirlanir()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("sayac");

            await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "Sayaç", password = "Test1234!" });

            await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod = "000000" });
            await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod = "111111" });

            var yanlisSonrasi = await KullaniciAsync(eposta);
            Assert.Equal(2, yanlisSonrasi.DogrulamaDenemeSayisi);

            var kod = SahteEpostaGonderici.Ortak.SonKod(eposta);
            var dogrula = await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod });

            Assert.Equal(HttpStatusCode.OK, dogrula.StatusCode);

            var sonrasi = await KullaniciAsync(eposta);
            Assert.Equal(0, sonrasi.DogrulamaDenemeSayisi);
        }

        private async Task<Garajim.Entity.Concrete.AppUser> KullaniciAsync(string eposta)
        {
            using var kapsam = _factory.Services.CreateScope();
            var db = kapsam.ServiceProvider.GetRequiredService<GarajimDbContext>();
            return await db.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == eposta);
        }
    }
}
