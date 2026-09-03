using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class KatalogHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public KatalogHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> GirisliAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Katalog Kullanıcısı", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [Fact]
        public async Task MarkalarGirisSiz401Doner()
        {
            var cevap = await _factory.CreateClient().GetAsync("/api/Katalog/markalar");

            Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
        }

        [Fact]
        public async Task MarkalarElliAltiKayitDoner()
        {
            var client = await GirisliAsync("markalar");

            var cevap = await client.GetAsync("/api/Katalog/markalar");
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            var liste = belge.RootElement.GetProperty("data");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal(56, liste.GetArrayLength());
            Assert.Contains("Fiat", liste.EnumerateArray().Select(x => x.GetString()));
        }

        [Fact]
        public async Task SerilerMarkayaGoreDoner()
        {
            var client = await GirisliAsync("seriler");

            var cevap = await client.GetAsync("/api/Katalog/seriler?marka=Fiat");
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            var liste = belge.RootElement.GetProperty("data").EnumerateArray().Select(x => x.GetString()).ToList();

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Contains("Egea", liste);
            Assert.DoesNotContain("Corolla", liste);
        }

        [Fact]
        public async Task OlmayanMarkaDortYuzDort()
        {
            var client = await GirisliAsync("olmayan");

            var cevap = await client.GetAsync("/api/Katalog/seriler?marka=Yokmarka");

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task YanitBirSaatOnbelleklenir()
        {
            var client = await GirisliAsync("onbellek");

            var cevap = await client.GetAsync("/api/Katalog/markalar");

            Assert.NotNull(cevap.Headers.CacheControl);
            Assert.True(cevap.Headers.CacheControl.Private);
            Assert.Equal(TimeSpan.FromHours(1), cevap.Headers.CacheControl.MaxAge);
        }
    }
}
