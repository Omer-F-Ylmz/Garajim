namespace Garajim.Tests.Unit
{
    public class YonetimSayfasiTests
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
        public void YonetimSayfasiVarVeSatirIciScriptTasimaz()
        {
            var html = Oku("yonetim.html");

            Assert.Contains("yonetim.js", html);
            Assert.DoesNotContain("<script>", html);
            Assert.Contains("id=\"ozet-kartlar\"", html);
            Assert.Contains("id=\"geri-bildirim-liste\"", html);
        }

        [Fact]
        public void YonetimTextContentIleCizilir()
        {
            var js = Oku("yonetim.js");

            Assert.DoesNotContain("innerHTML", js);
            Assert.Contains("document.createElement", js);
            Assert.Contains("/api/Yonetim/ozet", js);
        }

        [Fact]
        public void YonetimServiceWorkerDisindadir()
        {
            var sw = Oku("sw.js");

            Assert.DoesNotContain("\"/yonetim.html\"", sw);
            Assert.Contains("url.pathname.indexOf(\"/yonetim\") === 0", sw);
        }
    }
}
