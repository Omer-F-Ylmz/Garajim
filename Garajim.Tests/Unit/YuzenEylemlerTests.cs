namespace Garajim.Tests.Unit
{
    public class YuzenEylemlerTests
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

        private static string Blok(string css, string secici)
        {
            var baslangic = css.IndexOf(secici + " {", StringComparison.Ordinal);
            Assert.True(baslangic > 0, secici + " tanımlı değil");

            var bitis = css.IndexOf('}', baslangic);
            return css.Substring(baslangic, bitis - baslangic);
        }

        [Fact]
        public void KazaAniVeGeriBildirimAyniYiginda()
        {
            var html = Oku("index.html");

            var yigin = html.IndexOf("id=\"yuzen-eylemler\"", StringComparison.Ordinal);
            var kapanis = html.IndexOf("</div>", yigin, StringComparison.Ordinal);
            var kaza = html.IndexOf("id=\"kaza-ani\"", StringComparison.Ordinal);
            var geri = html.IndexOf("id=\"geri-bildirim-btn\"", StringComparison.Ordinal);

            Assert.True(yigin > 0, "yüzen eylem yığını yok");
            Assert.InRange(kaza, yigin, kapanis);
            Assert.InRange(geri, yigin, kapanis);
        }

        [Fact]
        public void YiginSagAltaSabitlenirVeDikeyDizilir()
        {
            var blok = Blok(Oku("styles.css"), ".yuzen-eylemler");

            Assert.Contains("position: fixed", blok);
            Assert.Contains("right:", blok);
            Assert.Contains("bottom:", blok);
            Assert.Contains("flex-direction: column", blok);
        }

        [Fact]
        public void KazaAniYuvarlakDugmedir()
        {
            var blok = Blok(Oku("styles.css"), ".kaza-ani");

            Assert.Contains("border-radius: 50%", blok);
            Assert.DoesNotContain("width: 100%", blok);
        }

        [Fact]
        public void KazaAniArtikYapiskanDegil()
        {
            var css = Oku("styles.css");

            Assert.DoesNotContain("position: sticky", css.Split(".kaza-ani").Skip(1).FirstOrDefault() ?? string.Empty);
        }

        [Fact]
        public void IkonDugmesiErisilebilirAdTasir()
        {
            var html = Oku("index.html");
            var kaza = html.IndexOf("id=\"kaza-ani\"", StringComparison.Ordinal);
            var etiketSonu = html.IndexOf('>', kaza);
            var nitelikler = html.Substring(kaza, etiketSonu - kaza);

            Assert.Contains("aria-label=", nitelikler);
            Assert.Contains("title=", nitelikler);
        }

        [Fact]
        public void GovdeYuzenDugmelerinAltindaBosluBirakir()
        {
            var css = Oku("styles.css");
            var blok = Blok(css, "#app-screen");

            Assert.Contains("padding-bottom:", blok);
        }

        [Fact]
        public void ZIndexUstCubuktanDusukDegilAmaModalinAltinda()
        {
            var css = Oku("styles.css");

            var yigin = Blok(css, ".yuzen-eylemler");
            var modal = Blok(css, ".kaza-modal");

            var yiginZ = int.Parse(yigin.Split("z-index:")[1].Split(';')[0].Trim());
            var modalZ = int.Parse(modal.Split("z-index:")[1].Split(';')[0].Trim());

            Assert.True(yiginZ < modalZ, "yüzen yığın modalin üstünde kalmamalı");
        }
    }
}
