namespace Garajim.Tests.Unit
{
    public class TurkceBicimTests
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
        public void YuzdeIsaretiOnceGelir()
        {
            var app = Oku("app.js");

            Assert.DoesNotContain("+ \" %\"", app);
            Assert.Contains("function yuzde(", app);
        }

        [Fact]
        public void YonetimYuzdeleriTekBicimde()
        {
            var js = Oku("yonetim.js");

            Assert.DoesNotContain("+ \" %\"", js);
            Assert.Contains("yuzde(", js);
        }

        [Fact]
        public void YonetimTarihleriTurkceBicimde()
        {
            var js = Oku("yonetim.js");

            Assert.DoesNotContain("make(\"td\", gun.gun)", js);
            Assert.Contains("tarihMetni(gun.gun)", js);
        }

        [Fact]
        public void GeriBildirimTuruEtiketlenir()
        {
            var js = Oku("yonetim.js");

            Assert.Contains("TUR_ADI", js);
            Assert.Contains("Öneri", js);
        }

        [Fact]
        public void KarneToplamLitreOndalikGosterir()
        {
            var js = Oku("karne.js");
            var bas = js.IndexOf("Toplam litre", StringComparison.Ordinal);
            var satir = js.Substring(bas - 80, 200);

            Assert.DoesNotContain("wholeFormat.format(karne.yakitOzeti.toplamLitre)", satir);
            Assert.Contains("literFormat", satir);
        }

        [Fact]
        public void ArsivNedeniTurkceEtiketle()
        {
            var app = Oku("app.js");

            Assert.Contains("ARSIV_NEDENLERI", app);
            Assert.Contains("Satıldı", app);
            Assert.DoesNotContain("make(\"td\", arac.arsivNedeni || \"-\")", app);
        }
    }
}
