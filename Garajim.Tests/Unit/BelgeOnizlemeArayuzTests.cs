namespace Garajim.Tests.Unit
{
    public class BelgeOnizlemeArayuzTests
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
        public void OnizlemeModaliSayfadaVar()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"onizleme-modal\"", html);
            Assert.Contains("id=\"onizleme-govde\"", html);
            Assert.Contains("id=\"onizleme-onceki\"", html);
            Assert.Contains("id=\"onizleme-sonraki\"", html);
        }

        [Fact]
        public void GorselOnizlemesiDataUriKullanir()
        {
            var govde = Bolum(Oku("app.js"), "function onizlemeGoster(", "function onizlemeModaliKapat(");

            Assert.Contains("readAsDataURL", govde);
            Assert.DoesNotContain("createObjectURL", govde);
        }

        [Fact]
        public void PdfIcinIndirmeSunulur()
        {
            var govde = Bolum(Oku("app.js"), "function onizlemeGoster(", "function onizlemeModaliKapat(");

            Assert.Contains("application/pdf", govde);
            Assert.Contains("İndir", govde);
        }

        [Fact]
        public void OnizlemeInnerHtmlKullanmaz()
        {
            var govde = Bolum(Oku("app.js"), "function onizlemeAc(", "function bindOnizleme(");

            Assert.DoesNotContain("innerHTML", govde);
            Assert.Contains("document.createElement", govde);
        }

        [Fact]
        public void GaleriGezinmesiVar()
        {
            var govde = Bolum(Oku("app.js"), "function onizlemeAc(", "function bindOnizleme(");

            Assert.Contains("onizlemeSirasi", govde);
        }

        [Fact]
        public void KaydaBaglaUcuCagrilir()
        {
            var govde = Bolum(Oku("app.js"), "function belgeyiKaydaBagla(", "function bindOnizleme(");

            Assert.Contains("/bagla", govde);
            Assert.Contains("\"PUT\"", govde);
        }

        [Fact]
        public void BelgeSatirindaOnizleDugmesiVar()
        {
            var govde = Bolum(Oku("app.js"), "function loadDocuments(", "function fileSize(");

            Assert.Contains("Önizle", govde);
        }
    }
}
