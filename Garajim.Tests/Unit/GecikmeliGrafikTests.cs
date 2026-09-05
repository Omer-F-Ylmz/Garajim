namespace Garajim.Tests.Unit
{
    public class GecikmeliGrafikTests
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
        public void GrafikKitapligiIlkYuktenCikarildi()
        {
            var html = Oku("index.html");

            Assert.DoesNotContain("chart.umd.min.js", html);
        }

        [Fact]
        public void GrafikIhtiyacAninaYuklenir()
        {
            var app = Oku("app.js");

            Assert.Contains("function grafikKitapligi(", app);
            Assert.Contains("chart.umd.min.js", app);
            Assert.Contains("document.createElement(\"script\")", app);
        }

        [Fact]
        public void GrafikCizimleriKitapligiBekler()
        {
            var app = Oku("app.js");

            Assert.Contains("grafikKitapligi().then", app);
        }

        [Fact]
        public void KaynakCspListesindeKalir()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            var guvenlik = File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "Startup", "GuvenlikBasliklari.cs"));

            Assert.Contains("cdn.jsdelivr.net", guvenlik);
        }
    }
}
