namespace Garajim.Tests.Unit
{
    public class TanitimGenislemeTests
    {
        private static readonly string[] Gorseller = { "ss-fis.png", "ss-bakim.png", "ss-evrak.png", "ss-rapor.png" };

        private static DirectoryInfo Kok()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return kok;
        }

        private static string Oku(string dosya)
        {
            return File.ReadAllText(Path.Combine(Kok().FullName, "Garajim.API", "wwwroot", dosya));
        }

        [Fact]
        public void NasilCalisirUcAdimTasir()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"tanitim-nasil\"", html);

            var baslangic = html.IndexOf("id=\"tanitim-nasil\"", StringComparison.Ordinal);
            var bitis = html.IndexOf("</section>", baslangic, StringComparison.Ordinal);
            var blok = html.Substring(baslangic, bitis - baslangic);

            Assert.Equal(3, blok.Split("<li").Length - 1);
        }

        [Fact]
        public void OzellikKartlarindaUstaYakindaEtiketiVar()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("TANITIM_KARTLARI = [", StringComparison.Ordinal);
            var bitis = app.IndexOf("];", baslangic, StringComparison.Ordinal);
            var blok = app.Substring(baslangic, bitis - baslangic);

            Assert.Contains("yakında", blok, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("function tanitimKartlariniCiz()", app);
            Assert.Contains("kart-etiket", app);
        }

        [Fact]
        public void DortEkranGoruntusuVarVeAltMetinliDir()
        {
            var html = Oku("index.html");
            var klasor = Path.Combine(Kok().FullName, "Garajim.API", "wwwroot", "img");

            foreach (var gorsel in Gorseller)
            {
                var yol = Path.Combine(klasor, gorsel);

                Assert.True(File.Exists(yol), gorsel + " bulunamadı.");
                Assert.True(new FileInfo(yol).Length <= 150 * 1024, gorsel + " 150 KB'den büyük.");
                Assert.Contains("img/" + gorsel, html);
            }

            var baslangic = html.IndexOf("id=\"tanitim-ekranlar\"", StringComparison.Ordinal);
            var bitis = html.IndexOf("</section>", baslangic, StringComparison.Ordinal);
            var blok = html.Substring(baslangic, bitis - baslangic);

            Assert.Equal(Gorseller.Length, blok.Split("<img").Length - 1);
            Assert.Equal(Gorseller.Length, blok.Split(" alt=\"").Length - 1);
            Assert.Equal(Gorseller.Length, blok.Split("loading=\"lazy\"").Length - 1);
        }

        [Fact]
        public void PlanBolumuIkiPaketiAnlatir()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"tanitim-planlar\"", html);
            Assert.Contains("Bireysel", html);
            Assert.Contains("3 araç", html);
            Assert.Contains("Filo", html);
            Assert.Contains("İletişime geç", html);
        }

        [Fact]
        public void SssVeGuvenBolumleriYardimaBaglanir()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"tanitim-sss\"", html);
            Assert.Contains("id=\"tanitim-guven\"", html);

            var sss = html.IndexOf("id=\"tanitim-sss\"", StringComparison.Ordinal);
            var sssBitis = html.IndexOf("</section>", sss, StringComparison.Ordinal);
            var sssBlok = html.Substring(sss, sssBitis - sss);

            Assert.Equal(6, sssBlok.Split("<details").Length - 1);
            Assert.Contains("yardim.html", sssBlok);

            var guven = html.IndexOf("id=\"tanitim-guven\"", StringComparison.Ordinal);
            var guvenBitis = html.IndexOf("</section>", guven, StringComparison.Ordinal);
            var guvenBlok = html.Substring(guven, guvenBitis - guven);

            Assert.Contains("KVKK", guvenBlok);
            Assert.Contains("sartlar.html", guvenBlok);
        }

        [Fact]
        public void IletisimDestekAdresiniSunucudanAlir()
        {
            var html = Oku("index.html");
            var app = Oku("app.js");

            Assert.Contains("id=\"tanitim-destek\"", html);
            Assert.Contains("mailto:", app);
            Assert.Contains("/api/Yardim/sss", app);
        }

        [Fact]
        public void DemoVeUcretsizBaslaKorunur()
        {
            var html = Oku("index.html");

            Assert.Contains("Demo ile dene", html);
            Assert.Contains("Ücretsiz başla", html);
        }
    }
}
