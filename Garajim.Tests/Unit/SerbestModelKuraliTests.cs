using Garajim.Business.Katalog;

namespace Garajim.Tests.Unit
{
    public class SerbestModelKuraliTests
    {
        [Theory]
        [InlineData("Egea Cross")]
        [InlineData("Model S")]
        [InlineData("C4 Picasso")]
        [InlineData("Şahin 1.6")]
        [InlineData("XC-90")]
        [InlineData("500L")]
        public void GecerliModelAdlariKabulEdilir(string metin)
        {
            Assert.True(SerbestModelKurali.Gecerli(metin));
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("A")]
        [InlineData("1234")]
        [InlineData("aaaa")]
        [InlineData("Egeaaaa")]
        [InlineData("model!")]
        [InlineData("<script>")]
        [InlineData("aa@bb")]
        public void KuralDisiModelAdlariReddedilir(string metin)
        {
            Assert.False(SerbestModelKurali.Gecerli(metin));
        }

        [Fact]
        public void KirkKarakterUstuReddedilir()
        {
            var kirk = string.Concat(Enumerable.Repeat("abcd", 10));

            Assert.Equal(40, kirk.Length);
            Assert.True(SerbestModelKurali.Gecerli(kirk));
            Assert.False(SerbestModelKurali.Gecerli(kirk + "e"));
        }

        [Fact]
        public void MotorBosOlabilirAmaOtuzKarakteriAsamaz()
        {
            Assert.True(SerbestModelKurali.MotorGecerli(null));
            Assert.True(SerbestModelKurali.MotorGecerli("1.5 dCi"));
            Assert.False(SerbestModelKurali.MotorGecerli(string.Concat(Enumerable.Repeat("abcd", 8))));
            Assert.False(SerbestModelKurali.MotorGecerli("motor!"));
        }
    }
}
