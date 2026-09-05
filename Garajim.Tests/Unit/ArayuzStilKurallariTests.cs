namespace Garajim.Tests.Unit
{
    public class ArayuzStilKurallariTests
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
        public void DevreDisiAlanlarSolukGorunur()
        {
            var css = Oku("styles.css");

            Assert.Contains(":disabled", css);
            Assert.Contains("cursor: not-allowed", css);
        }

        [Fact]
        public void OdakHalkasiGorunur()
        {
            var css = Oku("styles.css");

            Assert.Contains(":focus-visible", css);
            Assert.Contains("outline-offset", css);
        }

        [Fact]
        public void DokunmaHedefleriEnAzKirkDortPiksel()
        {
            var css = Oku("styles.css");

            Assert.Contains("min-height: 44px", css);
            Assert.Contains(".onay-alan input", css);
        }

        [Fact]
        public void StilsizBaglantiRengiTanimli()
        {
            var css = Oku("styles.css");
            var bas = css.IndexOf("\na {", StringComparison.Ordinal);

            Assert.True(bas > 0, "genel a kuralı yok");
            Assert.Contains("var(--accent)", css.Substring(bas, css.IndexOf('}', bas) - bas));
        }

        [Fact]
        public void GridFormHucreleriUstenHizalanir()
        {
            var css = Oku("styles.css");
            var bas = css.IndexOf(".grid-form {", StringComparison.Ordinal);
            var blok = css.Substring(bas, css.IndexOf('}', bas) - bas);

            Assert.Contains("align-items: start", blok);
        }

        [Fact]
        public void FormEylemleriSarabilir()
        {
            var css = Oku("styles.css");
            var bas = css.IndexOf(".form-actions {", StringComparison.Ordinal);
            var blok = css.Substring(bas, css.IndexOf('}', bas) - bas);

            Assert.Contains("flex-wrap: wrap", blok);
        }

        [Fact]
        public void SekmeSeridiKaydirmaIpucuTasir()
        {
            var css = Oku("styles.css");

            Assert.Contains("scroll-snap-type", css);
            Assert.Contains(".tabs-sarmal::after", css);
        }

        [Fact]
        public void AcilKartTelefonuBuyukHedeftir()
        {
            var css = Oku("acil.css");

            Assert.Contains("min-height: 56px", css);
        }
    }
}
