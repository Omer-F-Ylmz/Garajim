using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class YakitMasrafDuzenlemeHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public YakitMasrafDuzenlemeHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int AracId)> HazirlaAsync(string on, string plaka)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Düzenleme", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Fiat",
                model = "Egea",
                year = 2019,
                currentKm = 40000,
                fuelType = "Dizel"
            });

            var govde = await arac.Content.ReadAsStringAsync();
            Assert.True(arac.IsSuccessStatusCode, govde);

            return (client, JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32());
        }

        private static async Task<int> YakitEkleAsync(HttpClient client, int aracId, int gunOnce, int km, decimal litre, bool tamDolum = true)
        {
            var cevap = await client.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId,
                date = DateTime.UtcNow.Date.AddDays(-gunOnce),
                liters = litre,
                totalCost = litre * 45m,
                km,
                tamDolum
            });

            var govde = await cevap.Content.ReadAsStringAsync();
            Assert.True(cevap.IsSuccessStatusCode, govde);

            return JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task<int> MasrafEkleAsync(HttpClient client, int aracId, int gunOnce, decimal tutar, string not)
        {
            var cevap = await client.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = aracId,
                category = "Otopark",
                date = DateTime.UtcNow.Date.AddDays(-gunOnce),
                amount = tutar,
                note = not
            });

            var govde = await cevap.Content.ReadAsStringAsync();
            Assert.True(cevap.IsSuccessStatusCode, govde);

            return JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        [Fact]
        public async Task YakitKaydiGuncellenir()
        {
            var (client, aracId) = await HazirlaAsync("yakduz", "34YD1001");
            var id = await YakitEkleAsync(client, aracId, 5, 41000, 40m);

            var cevap = await client.PutAsJsonAsync("/api/Fuel/" + id, new
            {
                date = DateTime.UtcNow.Date.AddDays(-4),
                liters = 42.5m,
                totalCost = 2100m,
                km = 41200,
                tamDolum = true
            });

            var govde = await cevap.Content.ReadAsStringAsync();
            Assert.True(cevap.IsSuccessStatusCode, govde);

            var veri = JsonDocument.Parse(govde).RootElement.GetProperty("data");
            Assert.Equal(41200, veri.GetProperty("km").GetInt32());
            Assert.Equal(42.5m, veri.GetProperty("liters").GetDecimal());
        }

        [Fact]
        public async Task YakitKmKomsulariniAsamaz()
        {
            var (client, aracId) = await HazirlaAsync("yakkm", "34YD1002");
            await YakitEkleAsync(client, aracId, 10, 41000, 40m);
            var ortaId = await YakitEkleAsync(client, aracId, 5, 41500, 38m);
            await YakitEkleAsync(client, aracId, 1, 42000, 39m);

            var cevap = await client.PutAsJsonAsync("/api/Fuel/" + ortaId, new
            {
                date = DateTime.UtcNow.Date.AddDays(-5),
                liters = 38m,
                totalCost = 1700m,
                km = 43000,
                tamDolum = true
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
            Assert.Contains("kilometre", (await cevap.Content.ReadAsStringAsync()).ToLowerInvariant());
        }

        [Fact]
        public async Task YakitKmOncekindenKucukOlamaz()
        {
            var (client, aracId) = await HazirlaAsync("yakkm2", "34YD1003");
            await YakitEkleAsync(client, aracId, 10, 41000, 40m);
            var ortaId = await YakitEkleAsync(client, aracId, 5, 41500, 38m);

            var cevap = await client.PutAsJsonAsync("/api/Fuel/" + ortaId, new
            {
                date = DateTime.UtcNow.Date.AddDays(-5),
                liters = 38m,
                totalCost = 1700m,
                km = 40500,
                tamDolum = true
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task YakitGuncellemesiSupheliBayraginiYenidenHesaplar()
        {
            var (client, aracId) = await HazirlaAsync("yaksup", "34YD1004");
            await YakitEkleAsync(client, aracId, 20, 40000, 40m);
            var ikinciId = await YakitEkleAsync(client, aracId, 10, 40050, 40m);

            var once = await client.GetFromJsonAsync<JsonDocument>($"/api/Fuel?vehicleId={aracId}");
            var supheliOnce = once.RootElement.GetProperty("data").EnumerateArray()
                .First(k => k.GetProperty("id").GetInt32() == ikinciId).GetProperty("supheliKm").GetBoolean();

            Assert.True(supheliOnce);

            var cevap = await client.PutAsJsonAsync("/api/Fuel/" + ikinciId, new
            {
                date = DateTime.UtcNow.Date.AddDays(-10),
                liters = 40m,
                totalCost = 1800m,
                km = 40500,
                tamDolum = true
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());

            var sonra = await client.GetFromJsonAsync<JsonDocument>($"/api/Fuel?vehicleId={aracId}");
            var supheliSonra = sonra.RootElement.GetProperty("data").EnumerateArray()
                .First(k => k.GetProperty("id").GetInt32() == ikinciId).GetProperty("supheliKm").GetBoolean();

            Assert.False(supheliSonra);
        }

        [Fact]
        public async Task BaskaSirketinYakitKaydiGuncellenemez()
        {
            var (yabanci, yabanciArac) = await HazirlaAsync("yakyab", "34YD1005");
            var id = await YakitEkleAsync(yabanci, yabanciArac, 5, 41000, 40m);

            var (client, _) = await HazirlaAsync("yakben", "34YD1006");

            var cevap = await client.PutAsJsonAsync("/api/Fuel/" + id, new
            {
                date = DateTime.UtcNow.Date.AddDays(-5),
                liters = 40m,
                totalCost = 1800m,
                km = 41000,
                tamDolum = true
            });

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task MasrafKaydiGuncellenir()
        {
            var (client, aracId) = await HazirlaAsync("masduz", "34YD1007");
            var id = await MasrafEkleAsync(client, aracId, 5, 250m, "otopark");

            var cevap = await client.PutAsJsonAsync("/api/Expenses/" + id, new
            {
                category = "Yikama",
                date = DateTime.UtcNow.Date.AddDays(-3),
                amount = 320m,
                note = "yıkama ve iç temizlik"
            });

            var govde = await cevap.Content.ReadAsStringAsync();
            Assert.True(cevap.IsSuccessStatusCode, govde);

            var veri = JsonDocument.Parse(govde).RootElement.GetProperty("data");
            Assert.Equal(320m, veri.GetProperty("amount").GetDecimal());
            Assert.Equal("yıkama ve iç temizlik", veri.GetProperty("note").GetString());
        }

        [Fact]
        public async Task MasrafGelecekTarihReddedilir()
        {
            var (client, aracId) = await HazirlaAsync("masgel", "34YD1008");
            var id = await MasrafEkleAsync(client, aracId, 5, 250m, "otopark");

            var cevap = await client.PutAsJsonAsync("/api/Expenses/" + id, new
            {
                category = "Otopark",
                date = DateTime.UtcNow.Date.AddDays(3),
                amount = 250m,
                note = "otopark"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task BaskaSirketinMasrafiGuncellenemez()
        {
            var (yabanci, yabanciArac) = await HazirlaAsync("masyab", "34YD1009");
            var id = await MasrafEkleAsync(yabanci, yabanciArac, 5, 250m, "otopark");

            var (client, _) = await HazirlaAsync("masben", "34YD1010");

            var cevap = await client.PutAsJsonAsync("/api/Expenses/" + id, new
            {
                category = "Otopark",
                date = DateTime.UtcNow.Date.AddDays(-5),
                amount = 250m,
                note = "otopark"
            });

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task AyniGunkuKayitlardaKomsulukSiraylaHesaplanir()
        {
            var (client, aracId) = await HazirlaAsync("yakayni", "34YD1011");
            await YakitEkleAsync(client, aracId, 5, 41000, 40m);
            var ortaId = await YakitEkleAsync(client, aracId, 5, 41300, 38m);
            await YakitEkleAsync(client, aracId, 5, 41600, 39m);

            var cevap = await client.PutAsJsonAsync("/api/Fuel/" + ortaId, new
            {
                date = DateTime.UtcNow.Date.AddDays(-5),
                liters = 38m,
                totalCost = 1750m,
                km = 41400,
                tamDolum = true
            });

            var govde = await cevap.Content.ReadAsStringAsync();
            Assert.True(cevap.IsSuccessStatusCode, govde);
            Assert.Equal(41400, JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("km").GetInt32());
        }

        [Fact]
        public async Task AyniGunkuKayitlardaKomsulukSinirlariKorunur()
        {
            var (client, aracId) = await HazirlaAsync("yakayni2", "34YD1012");
            await YakitEkleAsync(client, aracId, 5, 41000, 40m);
            var ortaId = await YakitEkleAsync(client, aracId, 5, 41300, 38m);
            await YakitEkleAsync(client, aracId, 5, 41600, 39m);

            var cevap = await client.PutAsJsonAsync("/api/Fuel/" + ortaId, new
            {
                date = DateTime.UtcNow.Date.AddDays(-5),
                liters = 38m,
                totalCost = 1750m,
                km = 41700,
                tamDolum = true
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }
    }
}
