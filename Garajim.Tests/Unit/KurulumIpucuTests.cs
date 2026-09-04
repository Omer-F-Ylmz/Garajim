namespace Garajim.Tests.Unit
{
    public class KurulumIpucuTests
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
        public void AndroidYuklemeDugmesiVar()
        {
            var html = Oku("index.html");
            var app = Oku("app.js");

            Assert.Contains("id=\"pwa-yukle\"", html);
            Assert.Contains("beforeinstallprompt", app);
            Assert.Contains("prompt()", app);
        }

        [Fact]
        public void IosSeridiStandaloneDegilkenCikar()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("function iosSeridiniDegerlendir(", StringComparison.Ordinal);
            var bitis = app.IndexOf("function bindKurulumIpucu(", StringComparison.Ordinal);

            Assert.True(baslangic > 0 && bitis > baslangic);

            var govde = app.Substring(baslangic, bitis - baslangic);

            Assert.Contains("standalone", govde);
            Assert.Contains("Ana Ekrana Ekle", govde);
            Assert.DoesNotContain("innerHTML", govde);
        }

        [Fact]
        public void SeritOtuzGunKapaliKalir()
        {
            var app = Oku("app.js");

            Assert.Contains("IOS_IPUCU_GUN = 30", app);
            Assert.Contains("yerelYaz(IOS_IPUCU_ANAHTARI", app);
        }

        [Fact]
        public void PingAnonimVeVerisiz()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            var controller = File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "Controllers", "SaglikController.cs"));

            Assert.Contains("[HttpGet(\"ping\")]", controller);
            Assert.Contains("AllowAnonymous", controller);
            Assert.Contains("\"ok\"", controller);
        }
    }
}
