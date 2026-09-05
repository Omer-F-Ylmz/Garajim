using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Entity.Concrete;
using Garajim.Dal.Concrete.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class KayitKaynagiHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public KayitKaynagiHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(string Kaynak, string Detay)> KayitOlAsync(object govde, string eposta)
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", govde);

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());

            using var kapsam = _factory.Services.CreateScope();
            var baglam = kapsam.ServiceProvider.GetRequiredService<GarajimDbContext>();

            var kullanici = await baglam.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == eposta);
            var sirket = await baglam.Companies.IgnoreQueryFilters().SingleAsync(c => c.Id == kullanici.CompanyId);

            return (sirket.KayitKaynagi, sirket.KayitKaynagiDetay);
        }

        [Fact]
        public async Task UtmsizKayitDogrudanYazar()
        {
            var eposta = Eposta("kaynakyok");

            var kaynak = await KayitOlAsync(
                new { email = eposta, fullName = "Kaynak", password = "Test1234!" }, eposta);

            Assert.Equal(KayitKaynaklari.Dogrudan, kaynak.Kaynak);
            Assert.Null(kaynak.Detay);
        }

        [Fact]
        public async Task UtmliKayitKaynagiYazar()
        {
            var eposta = Eposta("kaynakrehber");

            var kaynak = await KayitOlAsync(new
            {
                email = eposta,
                fullName = "Kaynak",
                password = "Test1234!",
                kaynak = "rehber",
                kaynakDetay = "p0420-ariza-kodu-nedir-anlami-nedenleri-aciliyet"
            }, eposta);

            Assert.Equal("rehber", kaynak.Kaynak);
            Assert.Equal("p0420-ariza-kodu-nedir-anlami-nedenleri-aciliyet", kaynak.Detay);
        }

        [Fact]
        public async Task UzunKaynakKirpilir()
        {
            var eposta = Eposta("kaynakuzun");

            var kaynak = await KayitOlAsync(new
            {
                email = eposta,
                fullName = "Kaynak",
                password = "Test1234!",
                kaynak = new string('r', 200),
                kaynakDetay = new string('d', 400)
            }, eposta);

            Assert.Equal(KayitKaynaklari.KaynakUzunluk, kaynak.Kaynak.Length);
            Assert.Equal(KayitKaynaklari.DetayUzunluk, kaynak.Detay.Length);
        }

        [Fact]
        public async Task UygunsuzKaynakReddedilir()
        {
            var client = _factory.CreateClient();

            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new
            {
                email = Eposta("kaynakkufur"),
                fullName = "Kaynak",
                password = "Test1234!",
                kaynak = UygunsuzOrnek()
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task DavetliKayitDavetKaynagiOlur()
        {
            var sahipEposta = Eposta("kaynakdaveteden");
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = sahipEposta, fullName = "Davet eden", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var davet = await client.GetAsync("/api/Davet");
            var kod = JsonDocument.Parse(await davet.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("kod").GetString();

            var davetliEposta = Eposta("kaynakdavetli");

            var kaynak = await KayitOlAsync(new
            {
                email = davetliEposta,
                fullName = "Davetli",
                password = "Test1234!",
                davetKodu = kod
            }, davetliEposta);

            Assert.Equal(KayitKaynaklari.Davet, kaynak.Kaynak);
        }

        private static string UygunsuzOrnek()
        {
            return Garajim.Business.Katalog.UygunsuzIfadeFiltresi.Varsayilan.Kokler.First();
        }
    }
}
