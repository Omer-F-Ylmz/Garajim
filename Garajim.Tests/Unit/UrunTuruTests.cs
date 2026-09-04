namespace Garajim.Tests.Unit
{
    public class UrunTuruTests
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

        private static string TurBlogu()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("TUR_ADIMLARI = [", StringComparison.Ordinal);
            var bitis = app.IndexOf("];", baslangic, StringComparison.Ordinal);

            Assert.True(baslangic > 0, "TUR_ADIMLARI tanımlı olmalı.");
            return app.Substring(baslangic, bitis - baslangic);
        }

        [Fact]
        public void AltiAdimVardirVeUstaAdimiYoktur()
        {
            var blok = TurBlogu();
            var satirlar = blok.Split('\n').Where(s => s.TrimStart().StartsWith("[\"", StringComparison.Ordinal)).ToList();

            Assert.Equal(6, satirlar.Count);

            foreach (var hedef in new[] { "vehicle-select", "receipt-btn", "ayarlar-btn", "karne-btn" })
            {
                Assert.Contains("#" + hedef, blok);
            }

            Assert.DoesNotContain("usta", blok, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void OverlayVeHedefKutusuIsaretlemedeVar()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"tur-katman\"", html);
            Assert.Contains("id=\"tur-kutu\"", html);
            Assert.Contains("id=\"tur-baslik\"", html);
            Assert.Contains("id=\"tur-metin\"", html);
            Assert.Contains("id=\"tur-ileri\"", html);
            Assert.Contains("id=\"tur-geri\"", html);
            Assert.Contains("id=\"tur-kapat\"", html);
        }

        [Fact]
        public void KlavyeEscVeOklarBaglanir()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("function turKlavye(", StringComparison.Ordinal);
            var bitis = app.IndexOf("function turBagla(", StringComparison.Ordinal);

            Assert.True(baslangic > 0 && bitis > baslangic);

            var govde = app.Substring(baslangic, bitis - baslangic);

            Assert.Contains("Escape", govde);
            Assert.Contains("ArrowRight", govde);
            Assert.Contains("ArrowLeft", govde);
        }

        [Fact]
        public void TurTextContentIleCizilir()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("function turAdimiCiz(", StringComparison.Ordinal);
            var bitis = app.IndexOf("function turKlavye(", StringComparison.Ordinal);

            Assert.True(baslangic > 0 && bitis > baslangic);

            var govde = app.Substring(baslangic, bitis - baslangic);

            Assert.Contains("textContent", govde);
            Assert.DoesNotContain("innerHTML", govde);
        }

        [Fact]
        public void AyarlardaTuruTekrarGosterDugmesiVar()
        {
            var html = Oku("index.html");
            var app = Oku("app.js");

            Assert.Contains("id=\"tur-tekrar\"", html);
            Assert.Contains("Turu tekrar göster", html);
            Assert.Contains("el(\"tur-tekrar\").addEventListener", app);
        }
    }
}
