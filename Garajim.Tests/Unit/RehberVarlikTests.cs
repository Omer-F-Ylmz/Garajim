namespace Garajim.Tests.Unit
{
    public class RehberVarlikTests
    {
        private static string Oku(string dosya)
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "wwwroot", dosya));
        }

        [Fact]
        public void AramaBetigiTextContentIleCizer()
        {
            var js = Oku("rehber.js");

            Assert.Contains("document.createElement", js);
            Assert.Contains("textContent", js);
            Assert.DoesNotContain("innerHTML", js);
            Assert.DoesNotContain("outerHTML", js);
            Assert.DoesNotContain("document.write", js);
        }

        [Fact]
        public void AramaBetigiDizinDosyasindanOkur()
        {
            var js = Oku("rehber.js");

            Assert.Contains("/rehber/index.json", js);
            Assert.Contains("rehber-ara", js);
            Assert.Contains("rehber-sonuc", js);
        }

        [Fact]
        public void RehberStiliMobilOncelikli()
        {
            var css = Oku("rehber.css");

            Assert.Contains("max-width: 480px", css);
            Assert.Contains(".rehber-cta", css);
            Assert.Contains(".rehber-kirmizi", css);
        }

        [Fact]
        public void ServiceWorkerRehberiAgaGecirir()
        {
            Assert.Contains("url.pathname.indexOf(\"/rehber\") === 0", Oku("sw.js"));
        }

        [Fact]
        public void RobotsRehberiEngellemez()
        {
            var robots = Oku("robots.txt");

            Assert.DoesNotContain("Disallow: /rehber", robots);
            Assert.Contains("Sitemap:", robots);
        }
    }
}
