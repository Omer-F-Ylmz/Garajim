using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class ArsivKapsamiHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public ArsivKapsamiHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        [Fact]
        public async Task ArsivliAracinHatirlatmasiYaklasanlardaGorunmez()
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("arsivkapsam"), fullName = "Arşiv", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = "34AK9001",
                brand = "Fiat",
                model = "Egea",
                year = 2019,
                currentKm = 40000,
                fuelType = "Dizel"
            });

            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var hatirlatma = await client.PostAsJsonAsync("/api/Reminders", new
            {
                vehicleId = aracId,
                type = "Muayene",
                dueDate = DateTime.UtcNow.Date.AddDays(10)
            });

            Assert.True(hatirlatma.IsSuccessStatusCode, await hatirlatma.Content.ReadAsStringAsync());

            var oncesi = await client.GetAsync("/api/Reminders/upcoming?days=30");
            Assert.Contains("34AK9001", await oncesi.Content.ReadAsStringAsync());

            var arsivle = await client.PostAsJsonAsync("/api/Vehicles/" + aracId + "/arsiv", new { neden = "Satildi" });
            Assert.True(arsivle.IsSuccessStatusCode, await arsivle.Content.ReadAsStringAsync());

            var sonrasi = await client.GetAsync("/api/Reminders/upcoming?days=30");

            Assert.DoesNotContain("34AK9001", await sonrasi.Content.ReadAsStringAsync());
        }
    }
}
