namespace Garajim.Tests.Unit
{
    public class YuzenVeUstCubukTests
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
        public void MobildeUstCubukYapiskanDegil()
        {
            var css = Oku("styles.css");
            var mobil = css.Split("@media").LastOrDefault(b => b.Contains("max-width: 640px") && b.Contains(".topbar {"));

            Assert.NotNull(mobil);

            var bas = mobil.IndexOf(".topbar {", StringComparison.Ordinal);

            Assert.True(bas > 0, "mobil .topbar kuralı yok");
            Assert.Contains("position: static", mobil.Substring(bas, mobil.IndexOf('}', bas) - bas));
        }

        [Fact]
        public void YuzenDugmelerDarEkrandaSadeceSimgeGosterir()
        {
            var css = Oku("styles.css");

            Assert.Contains(".geri-bildirim-metin", css);
        }

        [Fact]
        public void SayfaAltindaYuzenDugmeKadarBosluk()
        {
            var css = Oku("styles.css");

            Assert.Contains("padding-bottom: 96px", css);
        }

        [Fact]
        public void TurHedefiGorunurAlanaKaydirilir()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function turAdimiCiz(", StringComparison.Ordinal);
            var govde = app.Substring(bas, 900);

            Assert.Contains("scrollIntoView", govde);
        }
    }
}
