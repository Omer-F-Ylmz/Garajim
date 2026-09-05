using Garajim.Business.Concrete.Import;
using Garajim.Business.Concrete.Parts;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Unit
{
    public class EtiketVeSezmeTests
    {
        [Fact]
        public void ParcaAdlariBaslikHarfiyleBaslar()
        {
            foreach (ParcaTuru tur in Enum.GetValues<ParcaTuru>())
            {
                var ad = ParcaAraliklari.Ad(tur);

                Assert.False(string.IsNullOrWhiteSpace(ad));
                Assert.Equal(char.ToUpper(ad[0], System.Globalization.CultureInfo.GetCultureInfo("tr-TR")), ad[0]);
            }
        }

        [Fact]
        public void DrivvoSezgisiDahaAzYanlisPozitifVerir()
        {
            var tablo = new CsvTablo
            {
                Basliklar = new List<string> { "tarih", "km", "litre", "tutar" }
            };

            Assert.Equal("Genel", ImportSablonlari.Sez(tablo));
        }

        [Fact]
        public void BirimFiyatSutunuTasiyanYakitCsvsiDrivvoSayilir()
        {
            var tablo = new CsvTablo
            {
                Basliklar = new List<string> { "Tarih", "Kilometre", "Litre", "Fiyat", "Toplam maliyet" }
            };

            Assert.Equal("Drivvo", ImportSablonlari.Sez(tablo));
        }

        [Fact]
        public void GarajimCiktisiTanininir()
        {
            var tablo = new CsvTablo
            {
                Basliklar = new List<string> { "Plaka", "Tarih", "Km", "Litre", "Tutar", "BirimFiyat", "TamDolum" }
            };

            Assert.Equal("Garajım dışa aktarımı", ImportSablonlari.Sez(tablo));
        }
    }
}
