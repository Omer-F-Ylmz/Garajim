using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class TekrarlayanHatirlatmaHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public TekrarlayanHatirlatmaHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int AracId)> HazirlaAsync(string on, string plaka)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Tekrar", password = "Test1234!" });
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

        private static async Task<JsonElement> ListeAsync(HttpClient client, int aracId)
        {
            var cevap = await client.GetAsync("/api/Reminders?vehicleId=" + aracId);
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.True(cevap.IsSuccessStatusCode, govde);

            return JsonDocument.Parse(govde).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task TekrarAyliHatirlatmaTamamlaninceYenisiAcilir()
        {
            var (client, aracId) = await HazirlaAsync("tekraray", "34TK1001");

            var ekle = await client.PostAsJsonAsync("/api/Reminders", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                dueDate = DateTime.UtcNow.Date.AddDays(10),
                note = "yağ değişimi",
                tekrarAy = 6
            });

            var govde = await ekle.Content.ReadAsStringAsync();
            Assert.True(ekle.IsSuccessStatusCode, govde);

            var id = JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var tamamla = await client.PutAsync($"/api/Reminders/{id}/complete", null);
            Assert.True(tamamla.IsSuccessStatusCode, await tamamla.Content.ReadAsStringAsync());

            var liste = await ListeAsync(client, aracId);
            var kayitlar = liste.EnumerateArray().ToList();

            Assert.Equal(2, kayitlar.Count);

            var yeni = kayitlar.Single(k => !k.GetProperty("isCompleted").GetBoolean());
            var beklenen = DateTime.UtcNow.Date.AddDays(10).AddMonths(6);

            Assert.Equal(beklenen.Date, yeni.GetProperty("dueDate").GetDateTime().Date);
            Assert.Equal(6, yeni.GetProperty("tekrarAy").GetInt32());
            Assert.Equal("yağ değişimi", yeni.GetProperty("note").GetString());
        }

        [Fact]
        public async Task TekrarKmliHatirlatmaTamamlaninceKmEklenir()
        {
            var (client, aracId) = await HazirlaAsync("tekrarkm", "34TK1002");

            var ekle = await client.PostAsJsonAsync("/api/Reminders", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                dueKm = 45000,
                note = "bakım",
                tekrarKm = 15000
            });

            var id = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            await client.PutAsync($"/api/Reminders/{id}/complete", null);

            var liste = await ListeAsync(client, aracId);
            var yeni = liste.EnumerateArray().Single(k => !k.GetProperty("isCompleted").GetBoolean());

            Assert.Equal(60000, yeni.GetProperty("dueKm").GetInt32());
            Assert.Equal(15000, yeni.GetProperty("tekrarKm").GetInt32());
        }

        [Fact]
        public async Task TekrarsizHatirlatmaTamamlaninceYenisiAcilmaz()
        {
            var (client, aracId) = await HazirlaAsync("tekrarsiz", "34TK1003");

            var ekle = await client.PostAsJsonAsync("/api/Reminders", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                dueDate = DateTime.UtcNow.Date.AddDays(10),
                note = "tek seferlik"
            });

            var id = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            await client.PutAsync($"/api/Reminders/{id}/complete", null);

            var liste = await ListeAsync(client, aracId);

            Assert.Equal(1, liste.GetArrayLength());
            Assert.True(liste[0].GetProperty("isCompleted").GetBoolean());
        }

        [Fact]
        public async Task IkinciTamamlamaIkinciKopyaUretmez()
        {
            var (client, aracId) = await HazirlaAsync("tekraridem", "34TK1004");

            var ekle = await client.PostAsJsonAsync("/api/Reminders", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                dueDate = DateTime.UtcNow.Date.AddDays(10),
                note = "idempotent",
                tekrarAy = 3
            });

            var id = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            await client.PutAsync($"/api/Reminders/{id}/complete", null);
            await client.PutAsync($"/api/Reminders/{id}/complete", null);

            var liste = await ListeAsync(client, aracId);

            Assert.Equal(2, liste.GetArrayLength());
        }

        [Fact]
        public async Task TekrarDegerleriSinirDisiOlamaz()
        {
            var (client, aracId) = await HazirlaAsync("tekrarsinir", "34TK1005");

            var ay = await client.PostAsJsonAsync("/api/Reminders", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                dueDate = DateTime.UtcNow.Date.AddDays(10),
                tekrarAy = 0
            });

            Assert.Equal(HttpStatusCode.BadRequest, ay.StatusCode);

            var km = await client.PostAsJsonAsync("/api/Reminders", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                dueKm = 45000,
                tekrarKm = -5
            });

            Assert.Equal(HttpStatusCode.BadRequest, km.StatusCode);
        }
    }
}
