using Garajim.Business.Concrete;

namespace Garajim.Tests.Unit
{
    public class PlakaDogrulayiciTests
    {
        [Theory]
        [InlineData("34 abc 123", "34ABC123")]
        [InlineData("  06AB1234  ", "06AB1234")]
        [InlineData("01-a-12345", "01A12345")]
        public void NormalizeBuyukHarfeCevirirVeAyiriciSiler(string girdi, string beklenen)
        {
            Assert.Equal(beklenen, PlakaDogrulayici.Normalize(girdi));
        }

        [Theory]
        [InlineData("34ABC123")]
        [InlineData("34 ABC 123")]
        [InlineData("06AB1234")]
        [InlineData("06AB123")]
        [InlineData("01A12345")]
        [InlineData("01A1234")]
        [InlineData("81ZZZ99")]
        public void GecerliTurkPlakalariKabulEdilir(string plaka)
        {
            Assert.True(PlakaDogrulayici.Gecerli(plaka, yabanci: false));
        }

        [Theory]
        [InlineData("82ABC123")]
        [InlineData("00ABC123")]
        [InlineData("34ABCD12")]
        [InlineData("34A123")]
        [InlineData("34A123456")]
        [InlineData("34AB12")]
        [InlineData("34AB12345")]
        [InlineData("34ABC1")]
        [InlineData("34ABC1234")]
        [InlineData("34123456")]
        [InlineData("ABC123")]
        [InlineData("")]
        [InlineData(null)]
        public void KuralDisiTurkPlakalariReddedilir(string plaka)
        {
            Assert.False(PlakaDogrulayici.Gecerli(plaka, yabanci: false));
        }

        [Theory]
        [InlineData("34ÇBC123")]
        [InlineData("34ĞBC123")]
        [InlineData("34İBC123")]
        [InlineData("34ÖBC123")]
        [InlineData("34ŞBC123")]
        [InlineData("34ÜBC123")]
        [InlineData("34ıBC123")]
        public void TurkceKarakterTasiyanPlakaReddedilir(string plaka)
        {
            Assert.False(PlakaDogrulayici.Gecerli(plaka, yabanci: false));
        }

        [Theory]
        [InlineData("ABC12")]
        [InlineData("B-MW 1234")]
        [InlineData("123456789012")]
        public void YabanciPlakaSerbestAlfanumeriktir(string plaka)
        {
            Assert.True(PlakaDogrulayici.Gecerli(plaka, yabanci: true));
        }

        [Theory]
        [InlineData("ABC1")]
        [InlineData("1234567890123")]
        [InlineData("ABÇ1234")]
        public void YabanciPlakaSinirlariKorunur(string plaka)
        {
            Assert.False(PlakaDogrulayici.Gecerli(plaka, yabanci: true));
        }

        [Fact]
        public void DemoPlakasiTurkKuralinaUymaz()
        {
            Assert.False(PlakaDogrulayici.Gecerli("34DEMO34", yabanci: false));
        }
    }
}
