using Garajim.Entity.Dtos;

namespace Garajim.Tests.Unit
{
    public class ListeSorgusuTests
    {
        private static readonly string[] Alanlar = { "tarih", "tutar", "km" };

        [Fact]
        public void ParametresizIstekZarfIstemez()
        {
            Assert.False(new ListeSorgusu().ZarfIster);
        }

        [Theory]
        [InlineData("yag", null, null, null, null)]
        [InlineData(null, "tarih:desc", null, null, null)]
        [InlineData(null, null, 2, null, null)]
        [InlineData(null, null, null, 50, null)]
        public void HerhangiBirParametreZarfIster(string q, string sirala, int? sayfa, int? boyut, string yok)
        {
            var sorgu = new ListeSorgusu { Q = q, Sirala = sirala, Sayfa = sayfa, Boyut = boyut };

            Assert.True(sorgu.ZarfIster);
            Assert.Null(yok);
        }

        [Fact]
        public void YalnizTarihAraligiZarfIstemez()
        {
            Assert.False(new ListeSorgusu { Baslangic = new DateTime(2026, 1, 1) }.ZarfIster);
            Assert.False(new ListeSorgusu { Bitis = new DateTime(2026, 1, 1) }.ZarfIster);
        }

        [Fact]
        public void TarihAraligiSayfaylaBirlikteZarfIster()
        {
            Assert.True(new ListeSorgusu { Baslangic = new DateTime(2026, 1, 1), Sayfa = 1 }.ZarfIster);
        }

        [Theory]
        [InlineData(null, 25)]
        [InlineData(0, 25)]
        [InlineData(-3, 25)]
        [InlineData(10, 10)]
        [InlineData(100, 100)]
        [InlineData(101, 100)]
        [InlineData(5000, 100)]
        public void BoyutSinirlanir(int? girdi, int beklenen)
        {
            Assert.Equal(beklenen, new ListeSorgusu { Boyut = girdi }.GecerliBoyut());
        }

        [Theory]
        [InlineData(null, 1)]
        [InlineData(0, 1)]
        [InlineData(-2, 1)]
        [InlineData(7, 7)]
        public void SayfaEnAzBirdir(int? girdi, int beklenen)
        {
            Assert.Equal(beklenen, new ListeSorgusu { Sayfa = girdi }.GecerliSayfa());
        }

        [Fact]
        public void AramaMetniIkiYuzKarakteredeKirpilir()
        {
            var sorgu = new ListeSorgusu { Q = new string('a', 500) };

            Assert.Equal(ListeSorgusu.AramaUzunlugu, sorgu.GecerliQ().Length);
        }

        [Fact]
        public void BosAramaNullDoner()
        {
            Assert.Null(new ListeSorgusu { Q = "   " }.GecerliQ());
            Assert.Null(new ListeSorgusu().GecerliQ());
        }

        [Theory]
        [InlineData("tarih:desc", "tarih", false)]
        [InlineData("tarih:asc", "tarih", true)]
        [InlineData("TUTAR:DESC", "tutar", false)]
        [InlineData("km", "km", true)]
        [InlineData(null, "tarih", false)]
        public void SiralamaCozulur(string girdi, string beklenenAlan, bool beklenenArtan)
        {
            var sonuc = new ListeSorgusu { Sirala = girdi }.SiralamaCoz(Alanlar, "tarih");

            Assert.True(sonuc.Gecerli);
            Assert.Equal(beklenenAlan, sonuc.Alan);
            Assert.Equal(beklenenArtan, sonuc.Artan);
        }

        [Theory]
        [InlineData("plaka:desc")]
        [InlineData("tarih:yukari")]
        [InlineData("tarih:asc:ek")]
        public void GecersizSiralamaReddedilir(string girdi)
        {
            Assert.False(new ListeSorgusu { Sirala = girdi }.SiralamaCoz(Alanlar, "tarih").Gecerli);
        }

        [Fact]
        public void ZarfSayfaBilgisiniTasir()
        {
            var zarf = new SayfaliSonuc<int>(new List<int> { 1, 2, 3 }, 47, 2, 25);

            Assert.Equal(47, zarf.Toplam);
            Assert.Equal(2, zarf.Sayfa);
            Assert.Equal(25, zarf.Boyut);
            Assert.Equal(3, zarf.Kayitlar.Count);
        }
    }
}
