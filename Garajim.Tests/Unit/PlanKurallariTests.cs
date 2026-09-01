using Garajim.Business.Concrete.Planlar;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Unit
{
    public class PlanKurallariTests
    {
        private static PlanKurallari Kurallar(params (string Anahtar, string Deger)[] ayarlar)
        {
            var yapilandirma = new ConfigurationBuilder()
                .AddInMemoryCollection(ayarlar.Select(a => new KeyValuePair<string, string>(a.Anahtar, a.Deger)))
                .Build();
            return new PlanKurallari(yapilandirma);
        }

        [Fact]
        public void BireyselPlanVarsayilanLimitiUcArac()
        {
            Assert.Equal(3, Kurallar().AracLimiti(PlanType.Bireysel, null));
        }

        [Fact]
        public void FiloPlaniVarsayilanLimitiYirmiBesArac()
        {
            Assert.Equal(25, Kurallar().AracLimiti(PlanType.Filo, null));
        }

        [Fact]
        public void SirketLimitiPlanVarsayilaniniEzer()
        {
            Assert.Equal(7, Kurallar().AracLimiti(PlanType.Bireysel, 7));
        }

        [Fact]
        public void AyarlardanEzilebilir()
        {
            var kurallar = Kurallar(("Plan:BireyselAracLimiti", "5"), ("Plan:FiloAracLimiti", "100"));

            Assert.Equal(5, kurallar.AracLimiti(PlanType.Bireysel, null));
            Assert.Equal(100, kurallar.AracLimiti(PlanType.Filo, null));
        }

        [Theory]
        [InlineData("sifir")]
        [InlineData("0")]
        [InlineData("-4")]
        [InlineData("")]
        public void BozukAyarVarsayilanaDoner(string deger)
        {
            Assert.Equal(3, Kurallar(("Plan:BireyselAracLimiti", deger)).AracLimiti(PlanType.Bireysel, null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void GecersizSirketLimitiYokSayilir(int limit)
        {
            Assert.Equal(25, Kurallar().AracLimiti(PlanType.Filo, limit));
        }
    }
}
