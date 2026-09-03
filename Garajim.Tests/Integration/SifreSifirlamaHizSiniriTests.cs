using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class SifreSifirlamaHizSiniriTests : IDisposable
    {
        private const int Limit = 4;

        private sealed class DarLimitFactory : GarajimWebApplicationFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["RateLimiting:AuthPermitPerMinute"] = Limit.ToString()
                    });
                });
            }
        }

        private readonly DarLimitFactory _factory = new DarLimitFactory();

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task KodIstegiDakikalikSiniriAsincaDortYuzYirmiDokuz()
        {
            var client = _factory.CreateClient();
            var eposta = $"hizsinir-{Guid.NewGuid():N}@garajim.local";

            for (var i = 0; i < Limit; i++)
            {
                var izinli = await client.PostAsJsonAsync("/api/Auth/sifre-sifirla-kod", new { email = eposta });
                Assert.Equal(HttpStatusCode.OK, izinli.StatusCode);
            }

            var asan = await client.PostAsJsonAsync("/api/Auth/sifre-sifirla-kod", new { email = eposta });

            Assert.Equal(HttpStatusCode.TooManyRequests, asan.StatusCode);
        }
    }
}
