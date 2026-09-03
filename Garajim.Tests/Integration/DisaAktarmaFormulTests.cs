using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class DisaAktarmaFormulTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public DisaAktarmaFormulTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int AracId)> AracliSahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Dışa Aktaran", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Ford", model = "Focus", year = 2020, currentKm = 60000,
                fuelType = "Dizel", vites = "Manuel", kasaTipi = "Sedan"
            });
            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            return (client, aracId);
        }

        private static async Task<string> CsvAsync(HttpClient client, int aracId)
        {
            var cevap = await client.GetAsync($"/api/Export/masraf.csv?vehicleId={aracId}");
            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
            return await cevap.Content.ReadAsStringAsync();
        }

        [Theory]
        [InlineData("=cmd|'/c calc'!A1")]
        [InlineData("+1+1")]
        [InlineData("-2+3")]
        [InlineData("@SUM(1:2)")]
        public async Task FormulBaslangicliNotElektronikTabloyaFormulOlarakGitmez(string tehlikeli)
        {
            var (client, aracId) = await AracliSahipAsync("formul");

            await client.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = aracId,
                date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                category = "Otopark",
                amount = 150.75m,
                note = tehlikeli
            });

            var csv = await CsvAsync(client, aracId);

            Assert.DoesNotContain(";" + tehlikeli, csv);
            Assert.DoesNotContain(";\"" + tehlikeli, csv);
            Assert.Contains("'" + tehlikeli, csv);
        }

        [Fact]
        public async Task SayiVeMetinAlanlariBozulmaz()
        {
            var (client, aracId) = await AracliSahipAsync("formulsuz");

            await client.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = aracId,
                date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                category = "Otopark",
                amount = 150.75m,
                note = "Kapalı otopark"
            });

            var csv = await CsvAsync(client, aracId);

            Assert.Contains("150,75", csv);
            Assert.DoesNotContain("'150,75", csv);
            Assert.Contains("Kapalı otopark", csv);
            Assert.DoesNotContain("'Kapalı otopark", csv);
        }
    }
}
