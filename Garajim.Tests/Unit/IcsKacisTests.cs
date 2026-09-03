using System.Reflection;
using Garajim.Business.Concrete;

namespace Garajim.Tests.Unit
{
    public class IcsKacisTests
    {
        private static string Kacir(string metin)
        {
            var yontem = typeof(TakvimManager).GetMethod("Kacir", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(yontem);
            return (string)yontem.Invoke(null, new object[] { metin });
        }

        [Theory]
        [InlineData("34 ABC 123\r\nSUMMARY:sahte")]
        [InlineData("34 ABC 123\nSUMMARY:sahte")]
        [InlineData("34 ABC 123\rSUMMARY:sahte")]
        public void SatirSonuKarakterleriIcerikSatiriniBolemez(string girdi)
        {
            var sonuc = Kacir(girdi);

            Assert.DoesNotContain("\r", sonuc);
            Assert.DoesNotContain("\n", sonuc);
            Assert.Contains(@"\n", sonuc);
        }

        [Theory]
        [InlineData(";", @"\;")]
        [InlineData(",", @"\,")]
        [InlineData("\\", @"\\")]
        public void OzelKarakterlerKacirilir(string girdi, string beklenen)
        {
            Assert.Equal(beklenen, Kacir(girdi));
        }

        [Fact]
        public void DuzMetinDegismez()
        {
            Assert.Equal("Muayene bitiyor", Kacir("Muayene bitiyor"));
        }
    }
}
