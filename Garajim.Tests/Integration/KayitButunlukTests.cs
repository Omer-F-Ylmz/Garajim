using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Garajim.Dal.Concrete.Context;

namespace Garajim.Tests.Integration
{
    public class KayitButunlukTests : IDisposable
    {
        private readonly GarajimWebApplicationFactory _factory = new GarajimWebApplicationFactory();

        public void Dispose()
        {
            _factory.Dispose();
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<T> VeritabaniAsync<T>(Func<GarajimDbContext, Task<T>> is_)
        {
            using var kapsam = _factory.Services.CreateScope();
            return await is_(kapsam.ServiceProvider.GetRequiredService<GarajimDbContext>());
        }

        [Fact]
        public async Task CokUzunAdSoyadKayitOksuzSirketBirakmaz()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("uzunad");
            var uzunAd = new string('A', 250);

            var oncekiSirket = await VeritabaniAsync(db => db.Companies.IgnoreQueryFilters().CountAsync());

            var cevap = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = uzunAd, password = "Test1234!" });

            var sonrakiSirket = await VeritabaniAsync(db => db.Companies.IgnoreQueryFilters().CountAsync());
            var kullanici = await VeritabaniAsync(db => db.Users.IgnoreQueryFilters().CountAsync(u => u.Email == eposta));

            if (cevap.StatusCode == HttpStatusCode.Created)
            {
                Assert.Equal(1, kullanici);
                Assert.Equal(oncekiSirket + 1, sonrakiSirket);

                var kayitliAd = await VeritabaniAsync(db => db.Users.IgnoreQueryFilters()
                    .Where(u => u.Email == eposta).Select(u => u.FullName).SingleAsync());
                Assert.True(kayitliAd.Length <= 100, "Ad soyad kolon sınırına kırpılmalıydı: " + kayitliAd.Length);
            }
            else
            {
                Assert.Equal(0, kullanici);
                Assert.Equal(oncekiSirket, sonrakiSirket);
            }
        }

        [Fact]
        public async Task BasarisizKayitSirketOlusturmaz()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("cakisan");

            var ilk = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "İlk Kullanıcı", password = "Test1234!" });
            Assert.Equal(HttpStatusCode.Created, ilk.StatusCode);

            var oncekiSirket = await VeritabaniAsync(db => db.Companies.IgnoreQueryFilters().CountAsync());

            var ikinci = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "İkinci Kullanıcı", password = "Test1234!" });

            var sonrakiSirket = await VeritabaniAsync(db => db.Companies.IgnoreQueryFilters().CountAsync());

            Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);
            Assert.Equal(oncekiSirket, sonrakiSirket);
        }

        [Fact]
        public async Task AdSoyadVeSirketAdiKolonSinirinaKirpilir()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("kirpma");

            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new
            {
                email = eposta,
                fullName = new string('B', 180),
                companyName = new string('C', 300),
                password = "Test1234!"
            });

            Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);

            var kullanici = await VeritabaniAsync(db => db.Users.IgnoreQueryFilters()
                .SingleAsync(u => u.Email == eposta));
            var sirket = await VeritabaniAsync(db => db.Companies.IgnoreQueryFilters()
                .SingleAsync(c => c.Id == kullanici.CompanyId));

            Assert.Equal(100, kullanici.FullName.Length);
            Assert.Equal(150, sirket.Name.Length);
        }
    }
}
