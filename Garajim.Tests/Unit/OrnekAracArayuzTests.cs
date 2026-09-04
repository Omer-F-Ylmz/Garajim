namespace Garajim.Tests.Unit
{
    public class OrnekAracArayuzTests
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
        public void OrnekAracRozetiCizilir()
        {
            var app = Oku("app.js");

            Assert.Contains("vehicle.ornek", app);
            Assert.Contains("Örnek", app);
        }

        [Fact]
        public void KurulumCubugundaVeBosDurumdaDeneDugmesiVar()
        {
            var app = Oku("app.js");
            var html = Oku("index.html");

            Assert.Contains("function ornekAracOlustur(", app);
            Assert.Contains("Örnek araçla dene", html);
            Assert.Contains("id=\"kurulum-ornek\"", html);
            Assert.Contains("id=\"empty-ornek\"", html);
        }

        [Fact]
        public void AyarlardaOrnekAracKaldirilir()
        {
            var app = Oku("app.js");
            var html = Oku("index.html");

            Assert.Contains("id=\"ornek-sil\"", html);
            Assert.Contains("/api/Vehicles/ornek", app);
            Assert.Contains("\"DELETE\"", app);
        }
    }
}
