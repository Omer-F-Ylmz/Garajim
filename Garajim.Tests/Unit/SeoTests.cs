namespace Garajim.Tests.Unit
{
    public class SeoTests
    {
        private static DirectoryInfo Kok()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return kok;
        }

        private static string Oku(string dosya)
        {
            return File.ReadAllText(Path.Combine(Kok().FullName, "Garajim.API", "wwwroot", dosya));
        }

        [Theory]
        [InlineData("index.html")]
        [InlineData("yardim.html")]
        [InlineData("sartlar.html")]
        [InlineData("yenilikler.html")]
        public void SayfalarMetaVeCanonicalTasir(string dosya)
        {
            var html = Oku(dosya);

            Assert.Contains("name=\"description\"", html);
            Assert.Contains("rel=\"canonical\"", html);
            Assert.Contains("property=\"og:title\"", html);
            Assert.Contains("property=\"og:description\"", html);
            Assert.Contains("property=\"og:image\"", html);
        }

        [Fact]
        public void OgGorseliVarVeDogruBoyutta()
        {
            var yol = Path.Combine(Kok().FullName, "Garajim.API", "wwwroot", "img", "og.png");

            Assert.True(File.Exists(yol), "og.png bulunamadı.");

            var bayt = File.ReadAllBytes(yol);

            Assert.True(bayt.Length <= 300 * 1024, "og.png 300 KB'den büyük.");

            var genislik = (bayt[16] << 24) | (bayt[17] << 16) | (bayt[18] << 8) | bayt[19];
            var yukseklik = (bayt[20] << 24) | (bayt[21] << 16) | (bayt[22] << 8) | bayt[23];

            Assert.Equal(1200, genislik);
            Assert.Equal(630, yukseklik);
        }

        [Fact]
        public void RobotsOzelYollariKapatir()
        {
            var robots = Oku("robots.txt");

            Assert.Contains("User-agent: *", robots);
            Assert.Contains("Disallow: /api/", robots);
            Assert.Contains("Disallow: /yonetim", robots);
            Assert.Contains("Disallow: /karne.html", robots);
            Assert.Contains("Disallow: /acil.html", robots);
            Assert.Contains("Sitemap:", robots);
        }

        [Fact]
        public void SitemapStatikSayfalariListeler()
        {
            var sitemap = Oku("sitemap.xml");

            foreach (var sayfa in new[] { "/", "/yardim.html", "/sartlar.html", "/yenilikler.html" })
            {
                Assert.Contains(sayfa + "</loc>", sitemap);
            }

            Assert.DoesNotContain("yonetim", sitemap);
            Assert.DoesNotContain("karne", sitemap);
        }

        [Fact]
        public void ServiceWorkerSeoDosyalariniOnbellegeAlmaz()
        {
            var sw = Oku("sw.js");

            Assert.DoesNotContain("\"/robots.txt\"", sw);
            Assert.DoesNotContain("\"/sitemap.xml\"", sw);
        }
    }
}
