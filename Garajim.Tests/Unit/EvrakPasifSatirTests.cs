namespace Garajim.Tests.Unit
{
    public class EvrakPasifSatirTests
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

        private static string Govde()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function renderEvrakRows(", StringComparison.Ordinal);
            var son = app.IndexOf("function evrakiDuzenle(", bas, StringComparison.Ordinal);

            Assert.True(bas > 0 && son > bas);
            return app.Substring(bas, son - bas);
        }

        [Fact]
        public void PasifSatirIsaretlenir()
        {
            var govde = Govde();

            Assert.Contains("evrak-pasif", govde);
        }

        [Fact]
        public void PasifSatirGeriSayimYerineEtiketGosterir()
        {
            var govde = Govde();

            Assert.Contains("Geçersiz", govde);
        }

        [Fact]
        public void AktifKayitlarOnceSiralanir()
        {
            var govde = Govde();

            Assert.Contains("aktif", govde);
            Assert.Contains("sort(", govde);
        }

        [Fact]
        public void PasifSatirIcinStilVar()
        {
            Assert.Contains(".evrak-pasif", Oku("styles.css"));
        }

        [Fact]
        public void GecmisEvraktaGunSayisiNegatifYazilmaz()
        {
            var govde = Govde();

            Assert.Contains("gün geçti", govde);
        }
    }
}
