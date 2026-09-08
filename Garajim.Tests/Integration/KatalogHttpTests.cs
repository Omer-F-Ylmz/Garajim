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
        public async Task YanitBirGunOnbelleklenir()
        {
            var client = await GirisliAsync("onbellek");

            var cevap = await client.GetAsync("/api/Katalog/markalar");

            Assert.NotNull(cevap.Headers.CacheControl);
            Assert.True(cevap.Headers.CacheControl.Private);
            Assert.Equal(TimeSpan.FromDays(1), cevap.Headers.CacheControl.MaxAge);
        }

        [Fact]
        public async Task YanitKatalogSurumBasligiTasir()
        {
            var client = await GirisliAsync("surum");

            var cevap = await client.GetAsync("/api/Katalog/markalar");

            Assert.True(cevap.Headers.Contains("X-Katalog-Surum"));
            Assert.False(string.IsNullOrWhiteSpace(cevap.Headers.GetValues("X-Katalog-Surum").First()));
        }

        [Fact]
        public async Task MarkalarAramaZarfDoner()
        {
            var client = await GirisliAsync("marka-ara");

            var cevap = await client.GetAsync("/api/Katalog/markalar?q=fi");
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            var zarf = belge.RootElement.GetProperty("data");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.True(zarf.GetProperty("toplam").GetInt32() > 0);
            Assert.Equal(1, zarf.GetProperty("sayfa").GetInt32());
            Assert.Equal(50, zarf.GetProperty("boyut").GetInt32());
            Assert.Contains("Fiat", Adlar(zarf));
        }

        private static List<string> Adlar(JsonElement zarf) =>
            zarf.GetProperty("kayitlar").EnumerateArray().Select(x => x.GetProperty("ad").GetString()).ToList();

        [Fact]
        public async Task MarkaAramasiTurkceDuyarsizdir()
        {
            var client = await GirisliAsync("marka-tr");

            var cevap = await client.GetAsync("/api/Katalog/markalar?q=SKO");
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());

            Assert.Contains("Skoda", Adlar(belge.RootElement.GetProperty("data")));
        }

        [Fact]
        public async Task MarkalarIkinciSayfaKalaniDoner()
        {
            var client = await GirisliAsync("marka-sayfa");

            var cevap = await client.GetAsync("/api/Katalog/markalar?sayfa=2");
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            var zarf = belge.RootElement.GetProperty("data");

            Assert.Equal(56, zarf.GetProperty("toplam").GetInt32());
            Assert.Equal(2, zarf.GetProperty("sayfa").GetInt32());
            Assert.Equal(6, zarf.GetProperty("kayitlar").GetArrayLength());
        }

        [Fact]
        public async Task MarkalarParametresizDuzListeKorunur()
        {
            var client = await GirisliAsync("marka-duz");

            var cevap = await client.GetAsync("/api/Katalog/markalar");
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());

            Assert.Equal(JsonValueKind.Array, belge.RootElement.GetProperty("data").ValueKind);
        }

        [Fact]
        public async Task SerilerAramaZarfDoner()
        {
            var client = await GirisliAsync("seri-ara");

            var cevap = await client.GetAsync("/api/Katalog/seriler?marka=Fiat&q=eg");
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            var zarf = belge.RootElement.GetProperty("data");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Contains("Egea", Adlar(zarf));
            Assert.DoesNotContain("Corolla", Adlar(zarf));
        }

        [Fact]
        public async Task ZarfKayitlariTrBayragiTasir()
        {
            var client = await GirisliAsync("tr-bayragi");

            var cevap = await client.GetAsync("/api/Katalog/markalar?q=fi");
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            var ilk = belge.RootElement.GetProperty("data").GetProperty("kayitlar").EnumerateArray().First();

            Assert.False(string.IsNullOrWhiteSpace(ilk.GetProperty("ad").GetString()));
            Assert.True(ilk.GetProperty("tr").GetBoolean());
        }

        [Fact]
        public async Task ZarfDahaVarBayragiTasir()
        {
            var client = await GirisliAsync("daha-var");

            var ilkSayfa = await client.GetAsync("/api/Katalog/markalar?sayfa=1");
            var sonSayfa = await client.GetAsync("/api/Katalog/markalar?sayfa=2");

            using var ilk = JsonDocument.Parse(await ilkSayfa.Content.ReadAsStringAsync());
            using var son = JsonDocument.Parse(await sonSayfa.Content.ReadAsStringAsync());

            Assert.True(ilk.RootElement.GetProperty("data").GetProperty("dahaVar").GetBoolean());
            Assert.False(son.RootElement.GetProperty("data").GetProperty("dahaVar").GetBoolean());
        }

        [Fact]
        public async Task SerilerParametresizDuzListeKorunur()
        {
            var client = await GirisliAsync("seri-duz");

            var cevap = await client.GetAsync("/api/Katalog/seriler?marka=Fiat");
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());

            Assert.Equal(JsonValueKind.Array, belge.RootElement.GetProperty("data").ValueKind);
        }

        [Fact]
        public async Task AramaliMarkaEtiketiAyriOnbelleklenir()
        {
            var client = await GirisliAsync("marka-etiket");

            var duz = await client.GetAsync("/api/Katalog/markalar");
            var aramali = await client.GetAsync("/api/Katalog/markalar?q=fi");

            Assert.NotNull(duz.Headers.ETag);
            Assert.NotNull(aramali.Headers.ETag);
            Assert.NotEqual(duz.Headers.ETag.Tag, aramali.Headers.ETag.Tag);
        }
    }
}
