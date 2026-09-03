using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.ML.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class FiyatModeliGecikmeliYuklemeTests : IDisposable
    {
        private readonly GarajimWebApplicationFactory _factory = new GarajimWebApplicationFactory();

        public void Dispose() => _factory.Dispose();

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private Lazy<FiyatModeliSozlugu> Sozluk() => _factory.Services.GetRequiredService<Lazy<FiyatModeliSozlugu>>();

        private async Task<(HttpClient Client, int AracId)> AracliSahipAsync()
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("fiyatmodel"), fullName = "Model Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Fiat", model = "Egea", year = 2019, currentKm = 80000,
                fuelType = "Benzin", vites = "Manuel", kasaTipi = "Sedan"
            });
            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            return (client, aracId);
        }

        [Fact]
        public async Task AracListesiFiyatModeliniYuklemez()
        {
            var (client, _) = await AracliSahipAsync();

            var liste = await client.GetAsync("/api/Vehicles");
            Assert.True(liste.IsSuccessStatusCode, await liste.Content.ReadAsStringAsync());

            Assert.False(Sozluk().IsValueCreated);
        }

        [Fact]
        public async Task TahminIstendigindeModelYuklenir()
        {
            var (client, aracId) = await AracliSahipAsync();

            Assert.False(Sozluk().IsValueCreated);

            await client.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);

            Assert.True(Sozluk().IsValueCreated);
        }
    }
}
