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
            Assert.Equal(3, Kurallar().AracLimiti(PlanType.Bireysel, null, 0));
        }

        [Fact]
        public void FiloPlaniVarsayilanLimitiYirmiBesArac()
        {
            Assert.Equal(25, Kurallar().AracLimiti(PlanType.Filo, null, 0));
        }

        [Fact]
        public void SirketLimitiPlanVarsayilaniniEzer()
        {
            Assert.Equal(7, Kurallar().AracLimiti(PlanType.Bireysel, 7, 0));
        }

        [Fact]
        public void AyarlardanEzilebilir()
        {
            var kurallar = Kurallar(("Plan:BireyselAracLimiti", "5"), ("Plan:FiloAracLimiti", "100"));

            Assert.Equal(5, kurallar.AracLimiti(PlanType.Bireysel, null, 0));
            Assert.Equal(100, kurallar.AracLimiti(PlanType.Filo, null, 0));
        }

        [Theory]
        [InlineData("sifir")]
        [InlineData("0")]
        [InlineData("-4")]
        [InlineData("")]
        public void BozukAyarVarsayilanaDoner(string deger)
        {
            Assert.Equal(3, Kurallar(("Plan:BireyselAracLimiti", deger)).AracLimiti(PlanType.Bireysel, null, 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void GecersizSirketLimitiYokSayilir(int limit)
        {
            Assert.Equal(25, Kurallar().AracLimiti(PlanType.Filo, limit, 0));
        }

        [Theory]
        [InlineData(0, 3)]
        [InlineData(1, 4)]
        [InlineData(2, 5)]
        [InlineData(3, 6)]
        public void BireyselPlandaHerDavetBirAracEkler(int davetSayisi, int beklenen)
        {
            Assert.Equal(beklenen, Kurallar().AracLimiti(PlanType.Bireysel, null, davetSayisi));
        }

        [Fact]
        public void EkAracHakkiUstSinirdaDurur()
        {
            Assert.Equal(6, Kurallar().AracLimiti(PlanType.Bireysel, null, 10));
        }

        [Fact]
        public void FiloPlaninaDavetEkAracVermez()
        {
            Assert.Equal(25, Kurallar().AracLimiti(PlanType.Filo, null, 10));
        }

        [Fact]
        public void EkAracHakkiSirketLimitininUstuneEklenir()
        {
            Assert.Equal(9, Kurallar().AracLimiti(PlanType.Bireysel, 7, 2));
        }

        [Fact]
        public void EkAracUstSiniriAyarlardanEzilebilir()
        {
            var kurallar = Kurallar(("Plan:DavetMaxEkArac", "1"));

            Assert.Equal(4, kurallar.AracLimiti(PlanType.Bireysel, null, 5));
            Assert.Equal(1, kurallar.DavetMaxEkArac());
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("-2")]
        public void BozukEkAracAyariVarsayilanaDoner(string deger)
        {
            Assert.Equal(3, Kurallar(("Plan:DavetMaxEkArac", deger)).DavetMaxEkArac());
        }

        [Fact]
        public void EkAracUstSiniriSifirYapilabilir()
        {
            var kurallar = Kurallar(("Plan:DavetMaxEkArac", "0"));

            Assert.Equal(0, kurallar.DavetMaxEkArac());
            Assert.Equal(3, kurallar.AracLimiti(PlanType.Bireysel, null, 5));
        }

        [Fact]
        public void NegatifDavetSayisiYokSayilir()
        {
            Assert.Equal(3, Kurallar().AracLimiti(PlanType.Bireysel, null, -4));
        }
    }
}
