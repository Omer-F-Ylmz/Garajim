namespace Garajim.Tests.Unit
{
    public class KazaModaliTests
    {
        private static string AppJs()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "wwwroot", "app.js"));
        }

        private static string Govde()
        {
            var app = AppJs();
            var bas = app.IndexOf("function kazaModaliKlavye(", StringComparison.Ordinal);
            var son = app.IndexOf("function kazaDosyasiAc(", bas, StringComparison.Ordinal);

            Assert.True(bas > 0 && son > bas, "kazaModaliKlavye yok");
            return app.Substring(bas, son - bas);
        }

        [Fact]
        public void EscKapatir()
        {
            Assert.Contains("Escape", Govde());
        }

        [Fact]
        public void OdakModalIcindeTutulur()
        {
            var govde = Govde();

            Assert.Contains("Tab", govde);
            Assert.Contains("focus()", govde);
        }

        [Fact]
        public void AcilistaOdakIceriTasinir()
        {
            var app = AppJs();
            var bas = app.IndexOf("function kazaRehberiniAc(", StringComparison.Ordinal);
            var son = app.IndexOf("function kazaModaliKapat(", bas, StringComparison.Ordinal);

            Assert.True(bas > 0 && son > bas);

            var govde = app.Substring(bas, son - bas);

            Assert.Contains(".focus()", govde);
            Assert.Contains("kaza-modal-acik", govde);
        }

        [Fact]
        public void KapanistaOdakTetikleyiciyeDoner()
        {
            var app = AppJs();
            var bas = app.IndexOf("function kazaModaliKapat(", StringComparison.Ordinal);
            var son = app.IndexOf("function kazaModaliKlavye(", bas, StringComparison.Ordinal);

            Assert.True(bas > 0 && son > bas);

            var govde = app.Substring(bas, son - bas);

            Assert.Contains("kaza-ani", govde);
            Assert.Contains("focus()", govde);
        }

        [Fact]
        public void ArkaPlanKaydirmaKilitlenir()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            var css = File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "wwwroot", "styles.css"));

            Assert.Contains(".kaza-modal-acik", css);
            Assert.Contains("overflow: hidden", css);
        }
    }
}
