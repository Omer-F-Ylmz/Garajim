namespace Garajim.Tests.Unit
{
    public class YonetimYetkiEkraniTests
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
        public void YetkisizKullaniciyaPanellerGizlenir()
        {
            var js = Oku("yonetim.js");

            Assert.Contains("panelleriGizle", js);
        }

        [Fact]
        public void YetkiHatasindaGizlemeCagirilir()
        {
            var js = Oku("yonetim.js");
            var bas = js.IndexOf("function yukle(", StringComparison.Ordinal);
            var govde = js.Substring(bas);

            Assert.Contains("panelleriGizle()", govde);
        }

        [Fact]
        public void PanellerVarsayilanOlarakGizli()
        {
            var html = Oku("yonetim.html");
            var bas = html.IndexOf("id=\"yonetim-govde\"", StringComparison.Ordinal);

            Assert.True(bas > 0, "yonetim-govde sarmalayıcısı yok");
            Assert.Contains("hidden", html.Substring(bas - 60, 120));
        }
    }
}
