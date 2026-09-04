using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class OrnekAracHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public OrnekAracHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Örnek", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<JsonElement> AraclarAsync(HttpClient client)
        {
            var cevap = await client.GetAsync("/api/Vehicles");
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        private static async Task<int> OrnekOlusturAsync(HttpClient client)
        {
            var cevap = await client.PostAsync("/api/Vehicles/ornek", null);
            var govde = await cevap.Content.ReadAsStringAsync();
            Assert.True(cevap.IsSuccessStatusCode, govde);
            return JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Fiat",
                model = "Egea",
                year = 2020,
                currentKm = 40000,
                fuelType = "Dizel"
            });
            var govde = await cevap.Content.ReadAsStringAsync();
            Assert.True(cevap.IsSuccessStatusCode, govde);
            return JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        [Fact]
        public async Task OrnekAracKayitlariylaBirlikteOlusur()
        {
            var client = await SahipAsync("ornekolustur");
            var aracId = await OrnekOlusturAsync(client);

            var arac = (await AraclarAsync(client)).EnumerateArray().Single(a => a.GetProperty("id").GetInt32() == aracId);

            Assert.True(arac.GetProperty("ornek").GetBoolean());
            Assert.Equal("34ORN001", arac.GetProperty("plate").GetString());
            Assert.Equal("Fiat", arac.GetProperty("brand").GetString());
            Assert.Equal("Egea", arac.GetProperty("model").GetString());
            Assert.Equal(2019, arac.GetProperty("year").GetInt32());

            var yakit = JsonDocument.Parse(await (await client.GetAsync("/api/Fuel?vehicleId=" + aracId)).Content.ReadAsStringAsync())
                .RootElement.GetProperty("data");
            var bakim = JsonDocument.Parse(await (await client.GetAsync("/api/Maintenance?vehicleId=" + aracId)).Content.ReadAsStringAsync())
                .RootElement.GetProperty("data");
            var masraf = JsonDocument.Parse(await (await client.GetAsync("/api/Expenses?vehicleId=" + aracId)).Content.ReadAsStringAsync())
                .RootElement.GetProperty("data");
            var evrak = JsonDocument.Parse(await (await client.GetAsync("/api/Evrak?vehicleId=" + aracId)).Content.ReadAsStringAsync())
                .RootElement.GetProperty("data");

            Assert.True(yakit.GetArrayLength() >= 4, "Yakıt kaydı yetersiz.");
            Assert.True(yakit.EnumerateArray().All(y => y.GetProperty("tamDolum").GetBoolean()));
            Assert.Equal(2, bakim.GetArrayLength());
            Assert.Contains(bakim.EnumerateArray(), b => b.GetProperty("parcalar").GetArrayLength() > 0);
            Assert.Equal(1, masraf.GetArrayLength());
            Assert.Equal(1, evrak.GetArrayLength());
        }

        [Fact]
        public async Task EvrakYirmiGunSonraBitiyor()
        {
            var client = await SahipAsync("ornekevrak");
            var aracId = await OrnekOlusturAsync(client);

            var evrak = JsonDocument.Parse(await (await client.GetAsync("/api/Evrak?vehicleId=" + aracId)).Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").EnumerateArray().Single();

            var kalan = evrak.GetProperty("kalanGun").GetInt32();

            Assert.InRange(kalan, 19, 21);
        }

        [Fact]
        public async Task IkinciCagri409Doner()
        {
            var client = await SahipAsync("ornekikinci");
            await OrnekOlusturAsync(client);

            var cevap = await client.PostAsync("/api/Vehicles/ornek", null);

            Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        }

        [Fact]
        public async Task OrnekAracPlanLimitineSayilmaz()
        {
            var client = await SahipAsync("orneklimit");
            await OrnekOlusturAsync(client);

            await AracEkleAsync(client, "34ORN101");
            await AracEkleAsync(client, "34ORN102");
            await AracEkleAsync(client, "34ORN103");

            var dorduncu = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = "34ORN104",
                brand = "Fiat",
                model = "Egea",
                year = 2020,
                currentKm = 40000,
                fuelType = "Dizel"
            });

            Assert.False(dorduncu.IsSuccessStatusCode, "Örnek araç limite sayılmadığı için üç gerçek araç eklenebilmeli, dördüncü reddedilmeli.");
        }

        [Fact]
        public async Task OrnekAracKarnePaylasamaz()
        {
            var client = await SahipAsync("ornekkarne");
            var aracId = await OrnekOlusturAsync(client);

            var cevap = await client.PostAsJsonAsync("/api/Vehicles/" + aracId + "/karne", new { bakim = true });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task SilmeTemizler()
        {
            var client = await SahipAsync("orneksil");
            var aracId = await OrnekOlusturAsync(client);
            await AracEkleAsync(client, "34ORN201");

            var sil = await client.DeleteAsync("/api/Vehicles/ornek");
            Assert.Equal(HttpStatusCode.OK, sil.StatusCode);

            var araclar = (await AraclarAsync(client)).EnumerateArray().ToList();

            Assert.DoesNotContain(araclar, a => a.GetProperty("id").GetInt32() == aracId);
            Assert.Contains(araclar, a => a.GetProperty("plate").GetString() == "34ORN201");

            var tekrar = await client.PostAsync("/api/Vehicles/ornek", null);
            Assert.True(tekrar.IsSuccessStatusCode, "Silindikten sonra yeniden oluşturulabilmeli.");
        }

        [Fact]
        public async Task OrnekYokkenSilmeSessizGecer()
        {
            var client = await SahipAsync("orneksilbos");

            var sil = await client.DeleteAsync("/api/Vehicles/ornek");

            Assert.Equal(HttpStatusCode.OK, sil.StatusCode);
        }

        [Fact]
        public async Task SurucuOrnekAracAcamaz()
        {
            var sahip = await SahipAsync("ornekowner");
            var eposta = Eposta("ornekdriver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var surucu = _factory.CreateClient();
            var giris = await surucu.PostAsJsonAsync("/api/Auth/login",
                new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("token").GetString();
            surucu.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var cevap = await surucu.PostAsync("/api/Vehicles/ornek", null);

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }
    }
}
