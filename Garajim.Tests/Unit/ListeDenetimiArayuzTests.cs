namespace Garajim.Tests.Unit
{
    public class ListeDenetimiArayuzTests
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

        private static string Bolum(string metin, string bas, string son)
        {
            var i = metin.IndexOf(bas, StringComparison.Ordinal);
            Assert.True(i > 0, bas + " bulunamadı");

            var j = metin.IndexOf(son, i, StringComparison.Ordinal);
            Assert.True(j > i, son + " bulunamadı");

            return metin.Substring(i, j - i);
        }

        [Fact]
        public void ListeDenetimiAramayiGeciktirir()
        {
            var govde = Bolum(Oku("app.js"), "var LISTE_TARIH_ARALIKLARI", "\n    function listeDenetimiKur(");

            Assert.Contains("300", govde);
            Assert.Contains("setTimeout", govde);
            Assert.Contains("clearTimeout", govde);
        }

        [Fact]
        public void ListeDenetimiSorguyuZarfParametreleriyleKurar()
        {
            var govde = Bolum(Oku("app.js"), "var LISTE_TARIH_ARALIKLARI", "\n    function listeDenetimiKur(");

            Assert.Contains("sayfa=", govde);
            Assert.Contains("boyut=", govde);
            Assert.Contains("q=", govde);
            Assert.Contains("sirala=", govde);
            Assert.Contains("encodeURIComponent", govde);
        }

        [Fact]
        public void ListeDenetimiDurumuOturumDepolamadaTutar()
        {
            var govde = Bolum(Oku("app.js"), "var LISTE_TARIH_ARALIKLARI", "\n    function listeDenetimiKur(");

            Assert.Contains("sessionStorage", govde);
        }

        [Fact]
        public void ListeDenetimiInnerHtmlKullanmaz()
        {
            var govde = Bolum(Oku("app.js"), "var LISTE_TARIH_ARALIKLARI", "\n    function listeDenetimiKur(");

            Assert.DoesNotContain("innerHTML", govde);
            Assert.Contains("document.createElement", govde);
        }

        [Fact]
        public void ListeDenetimiToplamVeBosDurumGosterir()
        {
            var govde = Bolum(Oku("app.js"), "var LISTE_TARIH_ARALIKLARI", "\n    function listeDenetimiKur(");

            Assert.Contains("toplam", govde);
            Assert.Contains("Sonuç bulunamadı", govde);
        }

        [Fact]
        public void ListeDenetimiDarEkrandaDahaFazlaYuklerGenisEkrandaSayfaNumarasiVerir()
        {
            var govde = Bolum(Oku("app.js"), "var LISTE_TARIH_ARALIKLARI", "\n    function listeDenetimiKur(");

            Assert.Contains("Daha fazla yükle", govde);
            Assert.Contains("matchMedia", govde);
        }

        [Fact]
        public void SiralanabilirBasliklarErisilebilirlikBildirir()
        {
            var govde = Bolum(Oku("app.js"), "var LISTE_TARIH_ARALIKLARI", "\n    function listeDenetimiKur(");

            Assert.Contains("aria-sort", govde);
        }

        [Fact]
        public void BakimListesiDenetimeBaglandi()
        {
            var app = Oku("app.js");
            var govde = Bolum(app, "function loadMaintenance(", "\n    function duzenleButonu(");

            Assert.Contains("bakimDenetimi", govde);
        }

        [Fact]
        public void BakimTablosuAracCubuguTasir()
        {
            Assert.Contains("id=\"maintenance-liste-araclar\"", Oku("index.html"));
        }
    }
}
