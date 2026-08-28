namespace Garajim.Tests.Integration
{
    public class StaticCacheHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public StaticCacheHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/app.js")]
        [InlineData("/styles.css")]
        [InlineData("/index.html")]
        public async Task UygulamaDosyalariHerZamanDogrulanir(string yol)
        {
            var cevap = await _factory.CreateClient().GetAsync(yol);
            cevap.EnsureSuccessStatusCode();

            var cache = cevap.Headers.CacheControl;

            Assert.NotNull(cache);
            Assert.True(cache.NoCache, $"{yol} için no-cache bekleniyordu.");
            Assert.Null(cache.MaxAge);
        }

        [Theory]
        [InlineData("/garajim-logo.svg")]
        [InlineData("/garajim-icon-32.png")]
        public async Task GorsellerUzunSureOnbelleklenir(string yol)
        {
            var cevap = await _factory.CreateClient().GetAsync(yol);
            cevap.EnsureSuccessStatusCode();

            var cache = cevap.Headers.CacheControl;

            Assert.NotNull(cache);
            Assert.NotNull(cache.MaxAge);
            Assert.True(cache.MaxAge > TimeSpan.FromMinutes(30), $"{yol} için uzun önbellek bekleniyordu.");
        }

        [Fact]
        public async Task DegismeyenDosyaIcinIkinciIstek304Doner()
        {
            var client = _factory.CreateClient();
            var ilk = await client.GetAsync("/app.js");
            var etag = ilk.Headers.ETag;

            Assert.NotNull(etag);

            var istek = new HttpRequestMessage(HttpMethod.Get, "/app.js");
            istek.Headers.IfNoneMatch.Add(etag);
            var ikinci = await client.SendAsync(istek);

            Assert.Equal(System.Net.HttpStatusCode.NotModified, ikinci.StatusCode);
        }
    }
}
