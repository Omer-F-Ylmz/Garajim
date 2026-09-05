namespace Garajim.Tests.Unit
{
    public class SekmeErisilebilirligiTests
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
        public void SekmeDugmeleriRolTasir()
        {
            var html = Oku("index.html");
            var bas = html.IndexOf("<nav class=\"tabs\" role=\"tablist\">", StringComparison.Ordinal);
            var blok = html.Substring(bas, html.IndexOf("</nav>", bas, StringComparison.Ordinal) - bas);
            var dugmeSayisi = System.Text.RegularExpressions.Regex.Matches(blok, "class=\"tab-btn").Count;

            Assert.True(dugmeSayisi > 10);
            Assert.Equal(dugmeSayisi, System.Text.RegularExpressions.Regex.Matches(blok, "role=\"tab\"").Count);
        }

        [Fact]
        public void SecimDurumuGuncellenir()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function selectTab(", StringComparison.Ordinal);
            var govde = app.Substring(bas, app.IndexOf("\n    }", bas, StringComparison.Ordinal) - bas);

            Assert.Contains("aria-selected", govde);
        }

        [Fact]
        public void OkTuslariSekmeDegistirir()
        {
            var app = Oku("app.js");

            Assert.Contains("function sekmeKlavye(", app);
            Assert.Contains("ArrowRight", app);
        }

        [Fact]
        public void AktifSekmeGorunurAlanaKaydirilir()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function selectTab(", StringComparison.Ordinal);
            var govde = app.Substring(bas, app.IndexOf("\n    }", bas, StringComparison.Ordinal) - bas);

            Assert.Contains("scrollIntoView", govde);
        }
    }
}
