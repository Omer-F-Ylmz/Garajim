using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class AuthRateLimitTests : IDisposable
    {
        private const int Esik = 3;

        private sealed class DarLimitliFactory : GarajimWebApplicationFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["RateLimiting:AuthPermitPerMinute"] = Esik.ToString()
                    });
                });
            }
        }

        private readonly DarLimitliFactory _factory = new DarLimitliFactory();

        [Fact]
        public async Task LoginUcunda_EsikAsilincaTurkceMesajla429Doner()
        {
            var client = _factory.CreateClient();
            var istek = new { email = "yok@garajim.local", password = "yanlis123" };

            var kodlar = new List<HttpStatusCode>();
            for (var i = 0; i < Esik; i++)
            {
                var izinli = await client.PostAsJsonAsync("/api/Auth/login", istek);
                kodlar.Add(izinli.StatusCode);
            }

            var asan = await client.PostAsJsonAsync("/api/Auth/login", istek);
            var govde = await asan.Content.ReadAsStringAsync();

            Assert.All(kodlar, kod => Assert.Equal(HttpStatusCode.Unauthorized, kod));
            Assert.Equal(HttpStatusCode.TooManyRequests, asan.StatusCode);
            Assert.Contains("Çok fazla deneme yaptınız", govde);
            Assert.Contains("\"success\":false", govde);
        }

        [Fact]
        public async Task RegisterUcuAyniLimitePaylasir()
        {
            var client = _factory.CreateClient();

            for (var i = 0; i < Esik; i++)
            {
                await client.PostAsJsonAsync("/api/Auth/register", new { email = $"kayit{i}@garajim.local", fullName = "Test", password = "gizli123" });
            }

            var asan = await client.PostAsJsonAsync("/api/Auth/register", new { email = "son@garajim.local", fullName = "Test", password = "gizli123" });

            Assert.Equal(HttpStatusCode.TooManyRequests, asan.StatusCode);
        }

        [Fact]
        public async Task KorumaliUclarLimitlenmez()
        {
            var client = _factory.CreateClient();

            var kodlar = new List<HttpStatusCode>();
            for (var i = 0; i < Esik * 4; i++)
            {
                var response = await client.GetAsync("/api/Vehicles");
                kodlar.Add(response.StatusCode);
            }

            Assert.All(kodlar, kod => Assert.Equal(HttpStatusCode.Unauthorized, kod));
        }

        [Fact]
        public async Task KokAdresLimitlenmez()
        {
            var client = _factory.CreateClient();

            var kodlar = new List<HttpStatusCode>();
            for (var i = 0; i < Esik * 4; i++)
            {
                var response = await client.GetAsync("/");
                kodlar.Add(response.StatusCode);
            }

            Assert.All(kodlar, kod => Assert.Equal(HttpStatusCode.OK, kod));
        }

        public void Dispose()
        {
            _factory.Dispose();
        }
    }
}
