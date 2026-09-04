namespace Garajim.Tests.Unit
{
    public class YardimSayfasiTests
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
        public void YardimSayfasiAramaVeListeTasir()
        {
            var html = Oku("yardim.html");

            Assert.Contains("id=\"sss-ara\"", html);
            Assert.Contains("id=\"sss-liste\"", html);
            Assert.Contains("yardim.js", html);
            Assert.DoesNotContain("<script>", html);
        }

        [Fact]
        public void YardimSayfasiDestekEpostasinaBaglanir()
        {
            var html = Oku("yardim.html");
            var js = Oku("yardim.js");

            Assert.Contains("id=\"destek-baglanti\"", html);
            Assert.Contains("mailto:", js);
        }

        [Fact]
        public void YardimListesiTextContentIleKurulurVeAnchorAcar()
        {
            var js = Oku("yardim.js");

            Assert.DoesNotContain("innerHTML", js);
            Assert.Contains("document.createElement", js);
            Assert.Contains("location.hash", js);
        }

        [Fact]
        public void SpaUstCubugundaYardimDugmesiVar()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"yardim-btn\"", html);
            Assert.Contains("yardim.html", html);
        }

        [Fact]
        public void YardimOnbellegeGirmez()
        {
            var sw = Oku("sw.js");

            Assert.DoesNotContain("\"/yardim.html\"", sw);
            Assert.DoesNotContain("\"/yardim.js\"", sw);
        }
    }
}
