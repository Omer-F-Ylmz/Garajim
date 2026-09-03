using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Garajim.Tests.Integration
{
    public class GuvenlikBaslikTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public GuvenlikBaslikTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static string Tek(HttpResponseMessage cevap, string ad)
        {
            Assert.True(cevap.Headers.TryGetValues(ad, out var degerler), ad + " başlığı yok.");
            return string.Join(" ", degerler);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/index.html")]
        [InlineData("/karne.html")]
        public async Task StatikSayfalarGuvenlikBasliklariniTasir(string yol)
        {
            var cevap = await _factory.CreateClient().GetAsync(yol);

            Assert.Equal("nosniff", Tek(cevap, "X-Content-Type-Options"));
            Assert.Equal("DENY", Tek(cevap, "X-Frame-Options"));
            Assert.Equal("no-referrer", Tek(cevap, "Referrer-Policy"));
        }

        [Fact]
        public async Task ApiYanitlariDaBasliklariTasir()
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("baslik"), fullName = "Başlık Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var cevap = await client.GetAsync("/api/Vehicles");

            Assert.Equal("nosniff", Tek(cevap, "X-Content-Type-Options"));
            Assert.Equal("DENY", Tek(cevap, "X-Frame-Options"));
        }

        [Fact]
        public async Task IcerikGuvenligiPolitikasiSaticiyiSinirlarVeSatiriciScriptiYasaklar()
        {
            var cevap = await _factory.CreateClient().GetAsync("/index.html");
            var csp = Tek(cevap, "Content-Security-Policy");

            Assert.Contains("frame-ancestors 'none'", csp);
            Assert.Contains("object-src 'none'", csp);
            Assert.Contains("base-uri 'self'", csp);
            Assert.Contains("default-src 'self'", csp);
            Assert.Contains("https://cdn.jsdelivr.net", csp);

            var scriptSrc = csp.Split(';').Single(p => p.Trim().StartsWith("script-src"));
            Assert.DoesNotContain("unsafe-inline", scriptSrc);
            Assert.DoesNotContain("unsafe-eval", scriptSrc);
        }

        [Fact]
        public async Task TutanakSayfasiKendiCspSiniAlir()
        {
            var cevap = await _factory.CreateClient().GetAsync("/api/Hasar/1/tutanak.html");

            Assert.Equal("nosniff", Tek(cevap, "X-Content-Type-Options"));
            Assert.Contains("frame-ancestors 'none'", Tek(cevap, "Content-Security-Policy"));
        }
    }
}
