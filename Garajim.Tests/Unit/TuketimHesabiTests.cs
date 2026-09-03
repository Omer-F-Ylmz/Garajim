using Garajim.Business.Concrete;
using Garajim.Entity.Concrete;

namespace Garajim.Tests.Unit
{
    public class TuketimHesabiTests
    {
        private static FuelRecord Kayit(int id, int km, decimal litre, bool tam = true, decimal? kwh = null)
        {
            return new FuelRecord
            {
                Id = id,
                VehicleId = 1,
                Km = km,
                Liters = litre,
                Kwh = kwh,
                TotalCost = litre * 40m,
                TamDolum = tam,
                Date = new DateTime(2026, 1, 1).AddDays(id)
            };
        }

        [Fact]
        public void TuketimYalnizArdisikTamDolumlarArasindaHesaplanir()
        {
            var kayitlar = new List<FuelRecord>
            {
                Kayit(1, 100000, 50m),
                Kayit(2, 100600, 42m)
            };

            var sonuc = TuketimHesabi.Hesapla(kayitlar);

            Assert.Equal(600, sonuc.OlculenKm);
            Assert.Equal(42m, sonuc.OlculenLitre);
            Assert.Equal(7.00m, sonuc.Litre100Km);
        }

        [Fact]
        public void AradakiKismiDolumlarinLitresiToplanir()
        {
            var kayitlar = new List<FuelRecord>
            {
                Kayit(1, 100000, 50m),
                Kayit(2, 100300, 20m, tam: false),
                Kayit(3, 100600, 22m)
            };

            var sonuc = TuketimHesabi.Hesapla(kayitlar);

            Assert.Equal(600, sonuc.OlculenKm);
            Assert.Equal(42m, sonuc.OlculenLitre);
            Assert.Equal(7.00m, sonuc.Litre100Km);
        }

        [Fact]
        public void KismiDolumlaBitenKuyrukOlcumeGirmez()
        {
            var kayitlar = new List<FuelRecord>
            {
                Kayit(1, 100000, 50m),
                Kayit(2, 100600, 42m),
                Kayit(3, 100900, 15m, tam: false)
            };

            var sonuc = TuketimHesabi.Hesapla(kayitlar);

            Assert.Equal(600, sonuc.OlculenKm);
            Assert.Equal(42m, sonuc.OlculenLitre);
        }

        [Fact]
        public void TekTamDolumTuketimUretmez()
        {
            var sonuc = TuketimHesabi.Hesapla(new List<FuelRecord> { Kayit(1, 100000, 50m) });

            Assert.Null(sonuc.Litre100Km);
            Assert.Equal(0, sonuc.OlculenKm);
        }

        [Theory]
        [InlineData(5, 300)]
        [InlineData(200, 300)]
        public void SinirDisiTuketimSupheliIsaretlenir(int litre, int aralik)
        {
            var kayitlar = new List<FuelRecord>
            {
                Kayit(1, 100000, 50m),
                Kayit(2, 100000 + aralik, litre)
            };

            var sonuc = TuketimHesabi.Hesapla(kayitlar);

            Assert.Contains(2, sonuc.SupheliKayitlar);
            Assert.Null(sonuc.Litre100Km);
        }

        [Fact]
        public void SupheliSegmentOrtalamayaGirmez()
        {
            var kayitlar = new List<FuelRecord>
            {
                Kayit(1, 100000, 50m),
                Kayit(2, 100600, 42m),
                Kayit(3, 100650, 45m),
                Kayit(4, 101250, 42m)
            };

            var sonuc = TuketimHesabi.Hesapla(kayitlar);

            Assert.Contains(3, sonuc.SupheliKayitlar);
            Assert.DoesNotContain(4, sonuc.SupheliKayitlar);
            Assert.Equal(1200, sonuc.OlculenKm);
            Assert.Equal(84m, sonuc.OlculenLitre);
            Assert.Equal(7.00m, sonuc.Litre100Km);
        }

        [Fact]
        public void ElektrikliAracKwhEsikleriyleOlculur()
        {
            var kayitlar = new List<FuelRecord>
            {
                Kayit(1, 50000, 0m, kwh: 40m),
                Kayit(2, 50250, 0m, kwh: 45m)
            };

            var sonuc = TuketimHesabi.Hesapla(kayitlar);

            Assert.Equal(18.00m, sonuc.Kwh100Km);
            Assert.Empty(sonuc.SupheliKayitlar);
        }

        [Fact]
        public void SinirDisiKwhSupheliIsaretlenir()
        {
            var kayitlar = new List<FuelRecord>
            {
                Kayit(1, 50000, 0m, kwh: 40m),
                Kayit(2, 50050, 0m, kwh: 45m)
            };

            var sonuc = TuketimHesabi.Hesapla(kayitlar);

            Assert.Contains(2, sonuc.SupheliKayitlar);
            Assert.Null(sonuc.Kwh100Km);
        }
    }
}
