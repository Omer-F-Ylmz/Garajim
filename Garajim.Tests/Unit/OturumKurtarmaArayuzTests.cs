namespace Garajim.Tests.Unit
{
    public class OturumKurtarmaArayuzTests
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
        public void OturumModaliSayfadaVar()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"oturum-modal\"", html);
            Assert.Contains("id=\"oturum-sifre\"", html);
            Assert.Contains("id=\"oturum-giris\"", html);
            Assert.Contains("id=\"oturum-cikis\"", html);
        }

        [Fact]
        public void OturumModaliErisilebilir()
        {
            var html = Oku("index.html");
            var bas = html.IndexOf("id=\"oturum-modal\"", StringComparison.Ordinal);
            var govde = html.Substring(bas, 800);

            Assert.Contains("role=\"dialog\"", govde);
            Assert.Contains("aria-modal=\"true\"", govde);
        }

        [Fact]
        public void DortYuzBirdeModalAcilirVeIstekKuyruklanir()
        {
            var govde = Bolum(Oku("app.js"), "function api(path, options)", "function readProblem(");

            Assert.Contains("oturumKuyruguna", govde);
            Assert.DoesNotContain("goToLogin(\"Oturum süreniz doldu", govde);
        }

        [Fact]
        public void KuyrukEnFazlaUcIstekTutar()
        {
            var govde = Bolum(Oku("app.js"), "var OTURUM_KUYRUK_SINIRI", "function oturumModaliKapat(");

            Assert.Contains("OTURUM_KUYRUK_SINIRI = 3", govde);
        }

        [Fact]
        public void BasariliGirisKuyruguTekrarOynatir()
        {
            var govde = Bolum(Oku("app.js"), "function oturumKuyrugunuOynat(", "function oturumModaliKapat(");

            Assert.Contains("saveSession", Oku("app.js"));
            Assert.Contains("api(", govde);
        }

        [Fact]
        public void FormTaslaklariOturumDepolamayaYazilir()
        {
            var govde = Bolum(Oku("app.js"), "function taslaklariKaydet(", "function taslaklariGeriYukle(");

            Assert.Contains("sessionStorage", govde);
        }

        [Fact]
        public void TaslaklarGirisSonrasiGeriYuklenir()
        {
            var app = Oku("app.js");

            Assert.Contains("taslaklariGeriYukle()", app);
        }

        [Fact]
        public void OturumModaliInnerHtmlKullanmaz()
        {
            var govde = Bolum(Oku("app.js"), "var OTURUM_KUYRUK_SINIRI", "function taslaklariGeriYukle(");

            Assert.DoesNotContain("innerHTML", govde);
        }

        [Fact]
        public void KuyrugaYalnizDegistirenIsteklerGirer()
        {
            var govde = Bolum(Oku("app.js"), "function oturumKuyruguna(", "function oturumModaliAc(");

            Assert.Contains("\"GET\"", govde);
            Assert.Contains("yontem", govde);
        }

        [Fact]
        public void KuyrukDolunincaEnEskisiDuser()
        {
            var govde = Bolum(Oku("app.js"), "function oturumKuyruguna(", "function oturumModaliAc(");

            Assert.Contains("shift()", govde);
        }
    }
}
