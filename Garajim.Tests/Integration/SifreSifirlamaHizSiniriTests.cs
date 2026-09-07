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

        private async Task<List<HttpStatusCode>> PatlamaAsync(int adet)
        {
            var client = _factory.CreateClient();
            var eposta = $"hizsinir-{Guid.NewGuid():N}@garajim.local";
            var durumlar = new List<HttpStatusCode>(adet);

            for (var i = 0; i < adet; i++)
            {
                var cevap = await client.PostAsJsonAsync("/api/Auth/sifre-sifirla-kod", new { email = eposta });
                durumlar.Add(cevap.StatusCode);
            }

            return durumlar;
        }

        [Fact]
        public async Task KodIstegiDakikalikSiniriAsincaDortYuzYirmiDokuz()
        {
            var durumlar = await PatlamaAsync(2 * Limit + 1);

            Assert.Contains(HttpStatusCode.TooManyRequests, durumlar);
        }

        [Fact]
        public async Task PencereBasinaEnCokLimitKadarIstekGecer()
        {
            var durumlar = await PatlamaAsync(2 * Limit + 1);

            var gecen = durumlar.Count(d => d == HttpStatusCode.OK);

            Assert.InRange(gecen, Limit, 2 * Limit);
        }

        [Fact]
        public async Task LimitAltindakiIstekGecer()
        {
            var durumlar = await PatlamaAsync(1);

            Assert.Equal(HttpStatusCode.OK, durumlar[0]);
        }

        [Fact]
        public async Task SinirAsiminda429DisindaBirDurumDonmez()
        {
            var durumlar = await PatlamaAsync(2 * Limit + 1);

            Assert.All(durumlar, d => Assert.True(
                d == HttpStatusCode.OK || d == HttpStatusCode.TooManyRequests,
                "Beklenmeyen durum: " + d));
        }
    }
}
