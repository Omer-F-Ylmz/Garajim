using System.Net;
using System.Text.RegularExpressions;

namespace Garajim.Tests.Integration
{
    public class OnbellekBasliklariTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public OnbellekBasliklariTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task KabukSayfasiVarlikAdreslerineSurumEkler()
        {
            var govde = await _factory.CreateClient().GetStringAsync("/index.html");

            Assert.Contains("app.js?v=", govde);
            Assert.Contains("styles.css?v=", govde);
            Assert.DoesNotContain("__SURUM__", govde);
        }

        [Fact]
        public async Task SurumluVarlikUzunSureOnbelleklenir()
        {
            var client = _factory.CreateClient();
            var govde = await client.GetStringAsync("/index.html");
            var surum = Regex.Match(govde, "app\\.js\\?v=([^\"']+)").Groups[1].Value;

            Assert.False(string.IsNullOrWhiteSpace(surum));

            var cevap = await client.GetAsync("/app.js?v=" + surum);
            var onbellek = cevap.Headers.CacheControl;

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.True(onbellek.Public);
            Assert.True(onbellek.MaxAge > TimeSpan.FromDays(300), "sürümlü varlık uzun önbelleklenmeli");
        }

        [Fact]
        public async Task SurumsuzVarlikHerSeferindeDogrulanir()
        {
            var cevap = await _factory.CreateClient().GetAsync("/app.js");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.True(cevap.Headers.CacheControl.NoCache);
        }

        [Fact]
        public async Task KabukSayfasiKendisiOnbelleklenmez()
        {
            var cevap = await _factory.CreateClient().GetAsync("/index.html");

            Assert.True(cevap.Headers.CacheControl.NoCache);
        }

        [Fact]
        public async Task KatalogYanitiEtagTasirVeIkinciIstek304Doner()
        {
            var client = _factory.CreateClient();
            await TestKayit.GirisYapAsync(client, "etag");

            var ilk = await client.GetAsync("/api/Katalog/markalar");
            var etag = ilk.Headers.ETag;

            Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);
            Assert.NotNull(etag);

            client.DefaultRequestHeaders.IfNoneMatch.Add(etag);
            var ikinci = await client.GetAsync("/api/Katalog/markalar");

            Assert.Equal(HttpStatusCode.NotModified, ikinci.StatusCode);
        }
    }
}
