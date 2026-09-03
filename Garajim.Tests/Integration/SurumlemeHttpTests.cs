using System.Net;

namespace Garajim.Tests.Integration
{
    public class SurumlemeHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public SurumlemeHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ServiceWorkerSurumYerTutucusuDoldurulur()
        {
            var cevap = await _factory.CreateClient().GetAsync("/sw.js");
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.DoesNotContain("__SURUM__", govde);
            Assert.Contains("garajim-kabuk-", govde);
        }

        [Fact]
        public async Task OnbellekAdiCalisanSurumuTasir()
        {
            var client = _factory.CreateClient();

            var kok = await client.GetAsync("/index.html");
            var surum = string.Join(string.Empty, kok.Headers.GetValues("X-App-Version"));

            var sw = await (await client.GetAsync("/sw.js")).Content.ReadAsStringAsync();

            Assert.False(string.IsNullOrWhiteSpace(surum));
            Assert.Contains("garajim-kabuk-\" + \"" + surum, sw);
        }

        [Fact]
        public async Task ServiceWorkerOnbellegeAlinmaz()
        {
            var cevap = await _factory.CreateClient().GetAsync("/sw.js");

            Assert.NotNull(cevap.Headers.CacheControl);
            Assert.True(cevap.Headers.CacheControl.NoCache);
        }

        [Fact]
        public async Task EskiOnbellekEtkinlestirmedeSilinir()
        {
            var sw = await (await _factory.CreateClient().GetAsync("/sw.js")).Content.ReadAsStringAsync();

            Assert.Contains("self.skipWaiting()", sw);
            Assert.Contains("self.clients.claim()", sw);
            Assert.Contains("caches.delete(ad)", sw);
        }

        [Fact]
        public async Task ApiYanitlariSurumBasligiTasir()
        {
            var cevap = await _factory.CreateClient().GetAsync("/api/Auth/login");

            Assert.True(cevap.Headers.Contains("X-App-Version"));
        }
    }
}
