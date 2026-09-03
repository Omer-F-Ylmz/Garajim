using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class HizSiniriBolumlemeTests : IDisposable
    {
        private const int Limit = 3;

        private sealed class DarLimitFactory : GarajimWebApplicationFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["RateLimiting:PahaliUcPerMinute"] = Limit.ToString()
                    });
                });
            }
        }

        private readonly DarLimitFactory _factory = new DarLimitFactory();

        public void Dispose() => _factory.Dispose();

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> KullaniciAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Kota Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<HttpStatusCode> IstekAsync(HttpClient client)
        {
            var cevap = await client.GetAsync("/api/Documents");
            return cevap.StatusCode;
        }

        private static async Task KotayiTuketAsync(HttpClient client)
        {
            for (var i = 0; i < Limit; i++)
            {
                var kod = await IstekAsync(client);
                Assert.NotEqual(HttpStatusCode.TooManyRequests, kod);
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, await IstekAsync(client));
        }

        [Fact]
        public async Task AyniIpdekiIkiKullaniciKotayiPaylasmaz()
        {
            var birinci = await KullaniciAsync("kota-bir");
            var ikinci = await KullaniciAsync("kota-iki");

            await KotayiTuketAsync(birinci);

            Assert.NotEqual(HttpStatusCode.TooManyRequests, await IstekAsync(ikinci));
        }

        [Fact]
        public async Task KimliksizIsteklerIpBasinaBolumlenir()
        {
            var birinci = _factory.CreateClient();
            var ikinci = _factory.CreateClient();

            for (var i = 0; i < Limit; i++)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, await IstekAsync(birinci));
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, await IstekAsync(ikinci));
        }
    }
}
