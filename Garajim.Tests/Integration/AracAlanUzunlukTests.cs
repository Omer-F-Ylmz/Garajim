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
    public class AracAlanUzunlukTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public AracAlanUzunlukTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static string Uzun(int uzunluk) => new string('K', uzunluk);

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Alan Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static object UzunArac(string plaka) => new
        {
            plate = plaka,
            brand = "Fiat",
            model = "Egea",
            year = 2020,
            currentKm = 40000,
            fuelType = "Benzin",
            vites = Uzun(60),
            kasaTipi = "Sedan",
            acilKisiAd = Uzun(200),
            acilKisiTelefon = Uzun(50),
            acilNot = Uzun(400)
        };

        private static void AlanlarSinirdaMi(JsonElement veri)
        {
            Assert.Equal("Fiat", veri.GetProperty("brand").GetString());
            Assert.Equal("Egea", veri.GetProperty("model").GetString());
            Assert.Equal(AracAlanUzunluklari.Vites, veri.GetProperty("vites").GetString().Length);
            Assert.Equal(AracAlanUzunluklari.AcilKisiAd, veri.GetProperty("acilKisiAd").GetString().Length);
            Assert.Equal(AracAlanUzunluklari.AcilKisiTelefon, veri.GetProperty("acilKisiTelefon").GetString().Length);
            Assert.Equal(AracAlanUzunluklari.AcilNot, veri.GetProperty("acilNot").GetString().Length);
        }

        [Fact]
        public async Task EklemedeUzunAlanlarKolonSinirinaKirpilir()
        {
            var client = await SahipAsync("aracalan");
            var plaka = TestPlaka.Uret();

            var cevap = await client.PostAsJsonAsync("/api/Vehicles", UzunArac(plaka));
            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());

            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            AlanlarSinirdaMi(belge.RootElement.GetProperty("data"));
        }

        [Fact]
        public async Task GuncellemedeUzunAlanlarKolonSinirinaKirpilir()
        {
            var client = await SahipAsync("aracalanput");
            var plaka = TestPlaka.Uret();

            var olustur = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka, brand = "Fiat", model = "Egea", year = 2020, currentKm = 40000,
                fuelType = "Benzin", vites = "Manuel", kasaTipi = "Sedan"
            });
            var aracId = JsonDocument.Parse(await olustur.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var guncelle = await client.PutAsJsonAsync($"/api/Vehicles/{aracId}", UzunArac(plaka));
            Assert.True(guncelle.IsSuccessStatusCode, await guncelle.Content.ReadAsStringAsync());

            var oku = await client.GetAsync($"/api/Vehicles/{aracId}");
            using var belge = JsonDocument.Parse(await oku.Content.ReadAsStringAsync());
            AlanlarSinirdaMi(belge.RootElement.GetProperty("data"));
        }

        [Fact]
        public async Task KolonaSigmayanPlakaReddedilir()
        {
            var client = await SahipAsync("aracplaka");

            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = "34" + Uzun(AracAlanUzunluklari.Plaka),
                brand = "Fiat", model = "Egea", year = 2020, currentKm = 40000,
                fuelType = "Benzin", vites = "Manuel", kasaTipi = "Sedan"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public void SabitlerKolonUzunluklariylaAyni()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();
            var arac = context.Model.FindEntityType(typeof(Vehicle));

            Assert.Equal(AracAlanUzunluklari.Plaka, arac.FindProperty(nameof(Vehicle.Plate)).GetMaxLength());
            Assert.Equal(AracAlanUzunluklari.Marka, arac.FindProperty(nameof(Vehicle.Brand)).GetMaxLength());
            Assert.Equal(AracAlanUzunluklari.Model, arac.FindProperty(nameof(Vehicle.Model)).GetMaxLength());
            Assert.Equal(AracAlanUzunluklari.Motor, arac.FindProperty(nameof(Vehicle.Motor)).GetMaxLength());
            Assert.Equal(AracAlanUzunluklari.Vites, arac.FindProperty(nameof(Vehicle.Vites)).GetMaxLength());
            Assert.Equal(AracAlanUzunluklari.AcilKisiAd, arac.FindProperty(nameof(Vehicle.AcilKisiAd)).GetMaxLength());
            Assert.Equal(AracAlanUzunluklari.AcilKisiTelefon, arac.FindProperty(nameof(Vehicle.AcilKisiTelefon)).GetMaxLength());
            Assert.Equal(AracAlanUzunluklari.AcilNot, arac.FindProperty(nameof(Vehicle.AcilNot)).GetMaxLength());
        }
    }
}
