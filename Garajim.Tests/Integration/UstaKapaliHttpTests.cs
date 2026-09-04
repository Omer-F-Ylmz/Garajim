using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class UstaKapaliHttpTests
    {
        private sealed class BayrakFactory : GarajimWebApplicationFactory
        {
            private readonly string _deger;

            public BayrakFactory(string deger)
            {
                _deger = deger;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Usta:Enabled"] = _deger
                }));
            }
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static async Task<HttpClient> SahipAsync(GarajimWebApplicationFactory f, string on)
        {
            var client = f.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Usta Bayrak", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [Fact]
        public async Task BayrakKapaliykenUstaUclari503Doner()
        {
            using var factory = new BayrakFactory("false");
            var client = await SahipAsync(factory, "kapali");

            var onay = await client.GetAsync("/api/Usta/onay");
            var govde = await onay.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.ServiceUnavailable, onay.StatusCode);
            Assert.Contains("yakında", govde, StringComparison.OrdinalIgnoreCase);

            var sohbet = await client.PostAsJsonAsync("/api/Usta/sohbet", new { vehicleId = 1 });
            Assert.Equal(HttpStatusCode.ServiceUnavailable, sohbet.StatusCode);
        }

        [Fact]
        public async Task BayrakAcikkenUstaUclariCalisir()
        {
            using var factory = new BayrakFactory("true");
            var client = await SahipAsync(factory, "acik");

            var onay = await client.GetAsync("/api/Usta/onay");

            Assert.Equal(HttpStatusCode.OK, onay.StatusCode);
        }

        [Fact]
        public async Task FisIstatistigiUstaDurumunuBildirir()
        {
            using var factory = new BayrakFactory("false");
            var client = await SahipAsync(factory, "durum");

            var cevap = await client.GetAsync("/api/Receipts/stats");
            var veri = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.False(veri.GetProperty("ustaAcik").GetBoolean());
        }
    }
}
