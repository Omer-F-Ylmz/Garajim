using Garajim.Business.Seed;

namespace Garajim.Tests.Unit
{
    public class TanitimSayfasiTests
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
        public void DemoDugmesiSeederKimligiyleAyniKalir()
        {
            var app = Oku("app.js");

            Assert.Contains("var DEMO_EPOSTA = \"" + DemoDataSeeder.DemoEmail + "\";", app);
            Assert.Contains("var DEMO_SIFRE = \"" + DemoDataSeeder.DemoPassword + "\";", app);
        }

        [Fact]
        public void AltiOzellikKartiVardirVeHepsiDoludur()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("TANITIM_KARTLARI = [", StringComparison.Ordinal);
            var bitis = app.IndexOf("];", baslangic, StringComparison.Ordinal);

            Assert.True(baslangic > 0);

            var blok = app.Substring(baslangic, bitis - baslangic);
            var satirlar = blok.Split('\n').Where(s => s.TrimStart().StartsWith("[\"", StringComparison.Ordinal)).ToList();

            Assert.Equal(6, satirlar.Count);
            Assert.All(satirlar, s => Assert.Contains("\", \"", s));

            foreach (var anahtar in new[] { "Fişi fotoğrafla", "karnesi", "Parça hafızası", "Evrak takvimi", "AI Usta", "Filo paketi" })
            {
                Assert.Contains(anahtar, blok);
            }
        }

        [Fact]
        public void TanitimGirisEkraninaGomuludurVeGirisSonrasiGizlenir()
        {
            var html = Oku("index.html");
            var app = Oku("app.js");

            var authBaslangic = html.IndexOf("<div id=\"auth-screen\"", StringComparison.Ordinal);
            var tanitim = html.IndexOf("<section id=\"tanitim\"", StringComparison.Ordinal);
            var authKart = html.IndexOf("<div class=\"auth-card\">", StringComparison.Ordinal);
            var appEkran = html.IndexOf("<div id=\"app-screen\"", StringComparison.Ordinal);

            Assert.True(authBaslangic > 0);
            Assert.True(tanitim > authBaslangic, "Tanıtım auth-screen içinde olmalı.");
            Assert.True(tanitim < authKart, "Tanıtım giriş kartından önce gelmeli.");
            Assert.True(tanitim < appEkran, "Tanıtım app-screen'den önce, auth-screen içinde kalmalı.");

            Assert.Contains("el(\"auth-screen\").classList.add(\"hidden\");", app);
        }

        [Fact]
        public void TanitimBaslikVeCagriMetinleriniTasir()
        {
            var html = Oku("index.html");

            Assert.Contains("Aracının belgeli hafızası", html);
            Assert.Contains("Fişi fotoğrafla, gerisini biz halledelim; sattığında karnesi yanında gider.", html);
            Assert.Contains("Demo ile dene", html);
            Assert.Contains("Ücretsiz başla", html);
            Assert.Contains("tanitim-davet-kod", html);
            Assert.Contains("/sartlar.html", html);
        }

        [Fact]
        public void TanitimDavetKoduKayitFormunaTasinir()
        {
            var app = Oku("app.js");

            Assert.Contains("el(\"register-davet\").value = kod;", app);
            Assert.Contains("function kayitSekmesineGec()", app);
        }

        [Fact]
        public void TanitimDugumleriTextContentIleKurulur()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("function tanitimKartlariniCiz()", StringComparison.Ordinal);
            var bitis = app.IndexOf("function kayitSekmesineGec()", StringComparison.Ordinal);

            Assert.True(baslangic > 0 && bitis > baslangic);

            var govde = app.Substring(baslangic, bitis - baslangic);

            Assert.Contains("document.createElement", govde);
            Assert.DoesNotContain("innerHTML", govde);
        }
    }
}
