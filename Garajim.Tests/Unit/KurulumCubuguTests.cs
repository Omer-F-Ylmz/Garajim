namespace Garajim.Tests.Unit
{
    public class KurulumCubuguTests
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
        public void CubukUcAdimiVeKapatmayiTasir()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"kurulum-cubugu\"", html);
            Assert.Contains("id=\"kurulum-adimlar\"", html);
            Assert.Contains("id=\"kurulum-gizle\"", html);
        }

        [Fact]
        public void CubukAppEkraninUstundeDurur()
        {
            var html = Oku("index.html");

            var appEkran = html.IndexOf("<div id=\"app-screen\"", StringComparison.Ordinal);
            var cubuk = html.IndexOf("id=\"kurulum-cubugu\"", StringComparison.Ordinal);
            var topbar = html.IndexOf("<header class=\"topbar\">", StringComparison.Ordinal);

            Assert.True(cubuk > appEkran, "Çubuk app-screen içinde olmalı.");
            Assert.True(cubuk < topbar, "Çubuk üst çubuğun önünde çizilmeli.");
        }

        [Fact]
        public void UcAdimVeHedefFormlariTanimli()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("KURULUM_ADIMLARI = [", StringComparison.Ordinal);
            var bitis = app.IndexOf("];", baslangic, StringComparison.Ordinal);

            Assert.True(baslangic > 0);

            var blok = app.Substring(baslangic, bitis - baslangic);

            Assert.Contains("aracVar", blok);
            Assert.Contains("ilkKayitVar", blok);
            Assert.Contains("evrakVar", blok);
        }

        [Fact]
        public void CubukTextContentIleKurulur()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("function kurulumCubuguCiz(", StringComparison.Ordinal);
            var bitis = app.IndexOf("function kurulumuGizle(", StringComparison.Ordinal);

            Assert.True(baslangic > 0 && bitis > baslangic);

            var govde = app.Substring(baslangic, bitis - baslangic);

            Assert.Contains("document.createElement", govde);
            Assert.DoesNotContain("innerHTML", govde);
        }

        [Fact]
        public void SurucudeCizilmez()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("function kurulumDurumunuYukle(", StringComparison.Ordinal);
            var bitis = app.IndexOf("function kurulumCubuguCiz(", StringComparison.Ordinal);

            Assert.True(baslangic > 0 && bitis > baslangic);

            Assert.Contains("canManage()", app.Substring(baslangic, bitis - baslangic));
        }
    }
}
