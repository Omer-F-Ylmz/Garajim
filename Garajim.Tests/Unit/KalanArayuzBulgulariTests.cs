namespace Garajim.Tests.Unit
{
    public class KalanArayuzBulgulariTests
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
        public void FiyatTahminiFormuSeciliAractanDolar()
        {
            var app = Oku("app.js");

            Assert.Contains("function tahminFormunuAractanDoldur(", app);
            Assert.Contains("KASA_TAHMIN_ESLEME", app);
        }

        [Fact]
        public void GeciciSifreKopyalanabilir()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("team-credential-password", StringComparison.Ordinal);

            Assert.True(bas > 0);
            Assert.Contains("baglantiKopyala", app.Substring(bas - 500, 1200));
        }

        [Fact]
        public void KarnedeBosBolumIcinAciklamaVar()
        {
            var js = Oku("karne.js");

            Assert.Contains("Bu araçta", js);
        }

        [Fact]
        public void KayitFormlariBeklerkenGeriBildirimVerir()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function formuKilitle(", StringComparison.Ordinal);
            var son = app.IndexOf("function api(", bas, StringComparison.Ordinal);
            var govde = app.Substring(bas, son - bas);

            Assert.Contains("aria-busy", govde);
            Assert.Contains("Kaydediliyor…", govde);
        }

        [Fact]
        public void TopluOkumaKilidiSabitSureyleAcilmaz()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function formuKilitle(", StringComparison.Ordinal);
            var son = app.IndexOf("function api(", bas, StringComparison.Ordinal);

            Assert.DoesNotContain("20000", app.Substring(bas, son - bas));
        }

        [Fact]
        public void AcilKartBosDurumAciklamasiTasir()
        {
            var js = Oku("acil.js");

            Assert.Contains("112", js);
        }

        [Fact]
        public void SurucudeOrnekAracBolumuGizlenir()
        {
            var app = Oku("app.js");

            Assert.Contains("ornek-bolumu", app);
        }

        [Fact]
        public void HesapSilmeAciklamasiRoleGoreDegisir()
        {
            var app = Oku("app.js");

            Assert.Contains("hesap-sil-aciklama", app);
        }

        [Fact]
        public void YardimDestekBaglantisiOlmadanGosterilmez()
        {
            var js = Oku("yardim.js");
            var bas = js.IndexOf("destek-baglanti", StringComparison.Ordinal);

            Assert.True(bas > 0);
            Assert.Contains("hidden", js.Substring(bas - 300, 900));
        }
    }
}
