using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class KucukIyilestirmeTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public KucukIyilestirmeTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int AracId)> AracliSahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Küçük Sahip", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(), brand = "Fiat", model = "Egea", year = 2020,
                currentKm = 100000, fuelType = "Dizel", vites = "Manuel", kasaTipi = "Sedan"
            });
            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            return (client, aracId);
        }

        [Fact]
        public async Task ParcaHatirlatmasiIkinciKezOlusturulmaz()
        {
            var (client, aracId) = await AracliSahipAsync("parca");

            for (var i = 0; i < 2; i++)
            {
                var bakim = await client.PostAsJsonAsync("/api/Maintenance", new
                {
                    vehicleId = aracId,
                    date = DateTime.UtcNow.Date.AddMonths(-12 + (i * 6)).ToString("yyyy-MM-dd"),
                    type = "PeriyodikBakim",
                    km = 80000 + (i * 10000),
                    cost = 3000m,
                    parcalar = new[] { new { parcaTuru = "MotorYagi", marka = "Castrol", adet = 1 } }
                });
                Assert.True(bakim.IsSuccessStatusCode, await bakim.Content.ReadAsStringAsync());
            }

            var birinci = await client.PostAsync($"/api/Vehicles/{aracId}/parca-hafizasi/MotorYagi/hatirlatma", null);

            Assert.True(birinci.IsSuccessStatusCode, await birinci.Content.ReadAsStringAsync());
            var ilkId = JsonDocument.Parse(await birinci.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetInt32();

            var ikinci = await client.PostAsync($"/api/Vehicles/{aracId}/parca-hafizasi/MotorYagi/hatirlatma", null);

            Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);

            using var belge = JsonDocument.Parse(await ikinci.Content.ReadAsStringAsync());
            Assert.Equal(ilkId, belge.RootElement.GetProperty("data").GetInt32());
        }

        [Fact]
        public async Task KarnePaylasimiVarsayilanDoksanGunSonraBiter()
        {
            var (client, aracId) = await AracliSahipAsync("karnegun");

            var karne = await client.PostAsJsonAsync($"/api/Vehicles/{aracId}/karne", new
            {
                kapsam = new
                {
                    bakimGecmisi = true, parcaHafizasi = false, yakitOzeti = false, belgeler = false,
                    plakaGoster = true, tutarGoster = false, acilKart = false, hasarGecmisi = false, beyanDegeri = false
                }
            });

            Assert.True(karne.IsSuccessStatusCode, await karne.Content.ReadAsStringAsync());

            using var belge = JsonDocument.Parse(await karne.Content.ReadAsStringAsync());
            var son = belge.RootElement.GetProperty("data").GetProperty("sonKullanma").GetDateTime();

            var beklenen = DateTime.UtcNow.AddDays(90);

            Assert.InRange(son, beklenen.AddMinutes(-5), beklenen.AddMinutes(5));
        }
    }
}
