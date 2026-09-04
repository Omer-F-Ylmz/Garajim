namespace Garajim.Tests.Unit
{
    public class GeriBildirimArayuzTests
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
        public void DugmeVeFormVar()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"geri-bildirim-btn\"", html);
            Assert.Contains("id=\"geri-bildirim-form\"", html);
            Assert.Contains("id=\"geri-bildirim-tur\"", html);
            Assert.Contains("maxlength=\"1000\"", html);
        }

        [Fact]
        public void SayfaVeSurumOtomatikGider()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("function geriBildirimGonder(", StringComparison.Ordinal);
            var bitis = app.IndexOf("function bindGeriBildirim(", StringComparison.Ordinal);

            Assert.True(baslangic > 0 && bitis > baslangic);

            var govde = app.Substring(baslangic, bitis - baslangic);

            Assert.Contains("sayfa: geriBildirimSayfasi()", govde);
            Assert.Contains("dataset.surum", govde);
            Assert.DoesNotContain("innerHTML", govde);
        }

        [Fact]
        public void UcTurTanimli()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("GERI_BILDIRIM_TURLERI = [", StringComparison.Ordinal);
            var bitis = app.IndexOf("];", baslangic, StringComparison.Ordinal);
            var blok = app.Substring(baslangic, bitis - baslangic);

            Assert.Contains("\"Hata\"", blok);
            Assert.Contains("\"Oneri\"", blok);
            Assert.Contains("\"Diger\"", blok);
        }
    }
}
