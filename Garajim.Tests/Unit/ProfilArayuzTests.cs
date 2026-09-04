namespace Garajim.Tests.Unit
{
    public class ProfilArayuzTests
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
        public void AyarlardaProfilBolumuVar()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"profil-form\"", html);
            Assert.Contains("id=\"profil-ad\"", html);
            Assert.Contains("id=\"profil-bildirim-evrak\"", html);
            Assert.Contains("id=\"profil-bildirim-hatirlatma\"", html);
        }

        [Fact]
        public void EpostaDegisimiIkiAdimlidir()
        {
            var html = Oku("index.html");
            var app = Oku("app.js");

            Assert.Contains("id=\"eposta-kod-form\"", html);
            Assert.Contains("id=\"eposta-degistir-form\"", html);
            Assert.Contains("/api/Account/eposta-degistir-kod", app);
            Assert.Contains("/api/Account/eposta-degistir\"", app);
        }

        [Fact]
        public void EpostaAlaniSaltOkunur()
        {
            var html = Oku("index.html");
            var satir = html.Split('\n').Single(s => s.Contains("id=\"profil-eposta\"", StringComparison.Ordinal));

            Assert.Contains("readonly", satir);
        }
    }
}
