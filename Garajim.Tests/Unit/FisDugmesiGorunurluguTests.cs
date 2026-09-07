namespace Garajim.Tests.Unit
{
    public class FisDugmesiGorunurluguTests
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

        private static string Govde()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function fisDugmesiniTazele(", StringComparison.Ordinal);

            Assert.True(bas > 0, "fisDugmesiniTazele bulunamadı");

            var son = app.IndexOf("\n    function ", bas + 10, StringComparison.Ordinal);

            Assert.True(son > bas);
            return app.Substring(bas, son - bas);
        }

        [Fact]
        public void DugmeGizliBaslar()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"receipt-btn\" class=\"primary compact hidden\"", html);
        }

        [Fact]
        public void AracYokkenVeBekleyenFisYokkenGizlenir()
        {
            var govde = Govde();

            Assert.Contains("state.vehicles", govde);
            Assert.Contains("bekleyenFisSayisi", govde);
            Assert.Contains("classList.toggle(\"hidden\"", govde);
        }

        [Fact]
        public void BekleyenFisVarkenGorunur()
        {
            var govde = Govde();

            Assert.Contains("aracVar || bekleyenVar", govde);
        }

        [Fact]
        public void GizliykenPanelDeKapanir()
        {
            var govde = Govde();

            Assert.Contains("el(\"receipt-box\").classList.add(\"hidden\")", govde);
        }

        [Fact]
        public void TiklamaAracsizDurumdaUyarir()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("el(\"receipt-btn\").addEventListener(\"click\"", StringComparison.Ordinal);

            Assert.True(bas > 0);
            Assert.Contains("araç zimmetlenmeli", app.Substring(bas, 400));
        }

        [Fact]
        public void ArayuzInnerHtmlKullanmaz()
        {
            Assert.DoesNotContain("innerHTML", Govde());
        }
    }
}
