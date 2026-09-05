using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class OzellikBayraklariHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public OzellikBayraklariHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Ozellik", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [Fact]
        public async Task SurucuDeOzellikBayraklariniOkuyabilir()
        {
            var sahip = await SahipAsync("ozellikowner");
            var eposta = Eposta("ozellikdriver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var surucu = _factory.CreateClient();
            var giris = await surucu.PostAsJsonAsync("/api/Auth/login",
                new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("token").GetString();
            surucu.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var cevap = await surucu.GetAsync("/api/Saglik/ozellikler");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var govde = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.True(govde.TryGetProperty("ustaAcik", out _));
        }

        [Fact]
        public async Task UstaKapaliykenBayrakYanlisDoner()
        {
            using var fabrika = new GarajimWebApplicationFactory();
            using var kapali = fabrika.WithWebHostBuilder(b => b.ConfigureAppConfiguration(c =>
                c.AddInMemoryCollection(new Dictionary<string, string> { ["Usta:Enabled"] = "false" })));

            var client = kapali.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("ustakapali"), fullName = "Ozellik", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var cevap = await client.GetAsync("/api/Saglik/ozellikler");
            var govde = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.False(govde.GetProperty("ustaAcik").GetBoolean());
        }
    }
}
