using System.Net;

namespace Garajim.Tests.Integration
{
    public class AramaKonsoluDogrulamaTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private const string Dosya = "googlea97199e856a67d08.html";

        private readonly GarajimWebApplicationFactory _factory;

        public AramaKonsoluDogrulamaTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task DogrulamaDosyasiIkiYuzDoner()
        {
            var client = _factory.CreateClient();

            var cevap = await client.GetAsync("/" + Dosya);

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        }

        [Fact]
        public async Task DogrulamaDosyasiIcerigiBirebirdir()
        {
            var client = _factory.CreateClient();

            var govde = await client.GetStringAsync("/" + Dosya);

            Assert.Equal("google-site-verification: " + Dosya + "\n", govde);
        }

        [Fact]
        public async Task DogrulamaDosyasiGirisIstemez()
        {
            var client = _factory.CreateClient();

            var cevap = await client.GetAsync("/" + Dosya);

            Assert.NotEqual(HttpStatusCode.Unauthorized, cevap.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, cevap.StatusCode);
        }

        [Fact]
        public async Task DogrulamaDosyasiSitemapeGirmez()
        {
            var client = _factory.CreateClient();

            var sitemap = await client.GetStringAsync("/sitemap.xml");

            Assert.DoesNotContain(Dosya, sitemap);
        }

        [Fact]
        public async Task RobotsDogrulamaDosyasiniEngellemez()
        {
            var client = _factory.CreateClient();

            var robots = await client.GetStringAsync("/robots.txt");

            Assert.DoesNotContain(Dosya, robots);
            Assert.Contains("Allow: /", robots);
        }

        [Fact]
        public void DogrulamaDosyasiKabukListesindeDegil()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);

            var sw = File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "wwwroot", "sw.js"));
            var bas = sw.IndexOf("KABUK_DOSYALARI", StringComparison.Ordinal);
            var son = sw.IndexOf("]", bas, StringComparison.Ordinal);

            Assert.True(bas > 0 && son > bas);
            Assert.DoesNotContain(Dosya, sw.Substring(bas, son - bas));
        }
    }
}
