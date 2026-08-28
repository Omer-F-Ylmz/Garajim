using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class TenantIsolationHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public TenantIsolationHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task<HttpClient> SirketIleGirisAsync(string eposta, string sirketAdi)
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = eposta, fullName = sirketAdi, password = "Test1234!" });
            var govde = await cevap.Content.ReadAsStringAsync();
            var token = JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2018,
                currentKm = 100000,
                fuelType = "Benzin"
            });

            var govde = await cevap.Content.ReadAsStringAsync();
            return JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        [Fact]
        public async Task BaskaSirketinAracinaGetPutDeleteHepsi404Doner()
        {
            var a = await SirketIleGirisAsync($"a-{Guid.NewGuid():N}@garajim.local", "A Filo");
            var b = await SirketIleGirisAsync($"b-{Guid.NewGuid():N}@garajim.local", "B Filo");
            var bAraci = await AracEkleAsync(b, "06BBB222");

            var getCevap = await a.GetAsync($"/api/Vehicles/{bAraci}");
            var putCevap = await a.PutAsJsonAsync($"/api/Vehicles/{bAraci}", new
            {
                brand = "Ele",
                model = "Gecirildi",
                year = 2020,
                currentKm = 1,
                fuelType = "Benzin"
            });
            var deleteCevap = await a.DeleteAsync($"/api/Vehicles/{bAraci}");

            Assert.Equal(HttpStatusCode.NotFound, getCevap.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, putCevap.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, deleteCevap.StatusCode);
        }

        [Fact]
        public async Task OlmayanAracVeYabanciAracAyniDurumKoduDoner()
        {
            var a = await SirketIleGirisAsync($"a-{Guid.NewGuid():N}@garajim.local", "A Filo");
            var b = await SirketIleGirisAsync($"b-{Guid.NewGuid():N}@garajim.local", "B Filo");
            var bAraci = await AracEkleAsync(b, "06BBB333");

            var yabanci = await a.GetAsync($"/api/Vehicles/{bAraci}");
            var olmayan = await a.GetAsync("/api/Vehicles/999999");

            Assert.Equal(olmayan.StatusCode, yabanci.StatusCode);
        }

        [Fact]
        public async Task ListeUcuYalnizKendiSirketiniDondurur()
        {
            var a = await SirketIleGirisAsync($"a-{Guid.NewGuid():N}@garajim.local", "A Filo");
            var b = await SirketIleGirisAsync($"b-{Guid.NewGuid():N}@garajim.local", "B Filo");
            await AracEkleAsync(a, "34AAA444");
            await AracEkleAsync(b, "06BBB444");

            var aListesi = await a.GetStringAsync("/api/Vehicles");
            var bListesi = await b.GetStringAsync("/api/Vehicles");

            Assert.Contains("34AAA444", aListesi);
            Assert.DoesNotContain("06BBB444", aListesi);
            Assert.Contains("06BBB444", bListesi);
            Assert.DoesNotContain("34AAA444", bListesi);
        }

        [Fact]
        public async Task BaskaSirketinAracinaKayitEklenemez()
        {
            var a = await SirketIleGirisAsync($"a-{Guid.NewGuid():N}@garajim.local", "A Filo");
            var b = await SirketIleGirisAsync($"b-{Guid.NewGuid():N}@garajim.local", "B Filo");
            var bAraci = await AracEkleAsync(b, "06BBB555");

            var cevap = await a.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = bAraci,
                type = "PeriyodikBakim",
                date = "2026-03-01",
                km = 110000,
                cost = 4500
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
            Assert.Contains("bulunamadı", await cevap.Content.ReadAsStringAsync());
        }
    }
}
