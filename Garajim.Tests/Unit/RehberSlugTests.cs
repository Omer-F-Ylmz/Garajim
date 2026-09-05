using Garajim.RehberUretici;

namespace Garajim.Tests.Unit
{
    public class RehberSlugTests
    {
        [Theory]
        [InlineData("Frene basınca metalik/gıcırtı sesi neden olur?", "frene-basinca-metalik-gicirti-sesi-neden-olur")]
        [InlineData("Fiat Egea 1.4 Fire bakım aralıkları", "fiat-egea-1-4-fire-bakim-araliklari")]
        [InlineData("P0420 arıza kodu nedir?", "p0420-ariza-kodu-nedir")]
        [InlineData("Şoför  koltuğu — ÇÖKME, ıslak", "sofor-koltugu-cokme-islak")]
        [InlineData("ÜÇ ÖĞÜN İĞNE", "uc-ogun-igne")]
        public void TurkceBaslikAsciiKebabOlur(string baslik, string beklenen)
        {
            Assert.Equal(beklenen, Slug.Uret(baslik));
        }

        [Fact]
        public void SlugSeksenKarakteriAsmaz()
        {
            var uzun = string.Join(" ", Enumerable.Repeat("çok uzun bir başlık", 20));

            var slug = Slug.Uret(uzun);

            Assert.True(slug.Length <= 80, slug.Length + " karakter");
            Assert.False(slug.EndsWith("-"));
        }

        [Fact]
        public void BosBaslikIdyeDuser()
        {
            Assert.Equal("blr-001", Slug.Uret("   ", "blr-001"));
            Assert.Equal("blr-001", Slug.Uret("!!! ???", "blr-001"));
        }

        [Fact]
        public void CakismadaIdEkiAlinir()
        {
            var kullanilan = new HashSet<string>(StringComparer.Ordinal);

            var ilk = Slug.Tekil("aynı başlık", "blr-001", kullanilan);
            var ikinci = Slug.Tekil("aynı başlık", "blr-002", kullanilan);

            Assert.Equal("ayni-baslik", ilk);
            Assert.Equal("ayni-baslik-blr-002", ikinci);
            Assert.NotEqual(ilk, ikinci);
        }
    }
}
