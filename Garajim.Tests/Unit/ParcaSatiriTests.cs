namespace Garajim.Tests.Unit
{
    public class ParcaSatiriTests
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

        private static string Govde(string bas, string son)
        {
            var app = AppJs();
            var b = app.IndexOf(bas, StringComparison.Ordinal);
            var s = app.IndexOf(son, b, StringComparison.Ordinal);

            Assert.True(b > 0 && s > b, bas + " bulunamadı");
            return app.Substring(b, s - b);
        }

        [Fact]
        public void BosParcaSatiriKaydaGirmez()
        {
            var govde = Govde("function readPartRows(", "function renderReceiptParts(");

            Assert.Contains("doluMu", govde);
        }

        [Fact]
        public void ParcaTutariTurkceOndalikKabulEder()
        {
            var govde = Govde("function addPartRow(", "function readPartRows(");

            Assert.DoesNotContain("tutar.type = \"number\"", govde);
            Assert.Contains("tutar.inputMode = \"decimal\"", govde);
        }

        [Fact]
        public void ParcaTutariSayiAlanIleOkunur()
        {
            var govde = Govde("function readPartRows(", "function renderReceiptParts(");

            Assert.Contains("sayiOku(", govde);
        }

        [Fact]
        public void ParcaAlanlariErisilebilirAdTasir()
        {
            var govde = Govde("function addPartRow(", "function readPartRows(");

            Assert.Contains("adet.setAttribute(\"aria-label\"", govde);
            Assert.Contains("tur.setAttribute(\"aria-label\"", govde);
            Assert.Contains("tutar.setAttribute(\"aria-label\"", govde);
        }

        [Fact]
        public void HasarBedeliDeOndalikMetinAlanidir()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            var html = File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "wwwroot", "index.html"));
            var bas = html.IndexOf("id=\"hasar-bedel\"", StringComparison.Ordinal);
            var etiket = html.Substring(html.LastIndexOf('<', bas), 200);

            Assert.Contains("inputmode=\"decimal\"", etiket);
            Assert.DoesNotContain("type=\"number\"", etiket);
        }
    }
}
