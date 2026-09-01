using Garajim.Business.Concrete.Parts;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Unit
{
    public class ParcaEslestiriciTests
    {
        [Theory]
        [InlineData("YAĞ FİLTRESİ", ParcaTuru.YagFiltresi)]
        [InlineData("yag filtresi", ParcaTuru.YagFiltresi)]
        [InlineData("MOTOR YAGI 5W30", ParcaTuru.MotorYagi)]
        [InlineData("5W-30 SENTETIK", ParcaTuru.MotorYagi)]
        [InlineData("ÖN BALATA TAKIMI", ParcaTuru.FrenBalatasiOn)]
        [InlineData("arka fren balatasi", ParcaTuru.FrenBalatasiArka)]
        [InlineData("ÖN FREN DİSKİ", ParcaTuru.FrenDiskiOn)]
        [InlineData("BUJİ TAKIMI", ParcaTuru.Buji)]
        [InlineData("TRİGER SETİ", ParcaTuru.TrigerSeti)]
        [InlineData("AKÜ 60AH", ParcaTuru.Aku)]
        [InlineData("LASTİK 195/65 R15", ParcaTuru.Lastik)]
        [InlineData("AMORTİSÖR ÖN", ParcaTuru.Amortisor)]
        [InlineData("SİLECEK TAKIMI", ParcaTuru.Silecek)]
        [InlineData("ANTİFRİZ", ParcaTuru.Antifriz)]
        [InlineData("FREN HİDROLİĞİ DOT4", ParcaTuru.FrenHidroligi)]
        [InlineData("DEVİRDAİM POMPASI", ParcaTuru.Devirdaim)]
        [InlineData("ROT BAŞI SAĞ", ParcaTuru.RotBasi)]
        [InlineData("POLEN FİLTRESİ", ParcaTuru.PolenFiltresi)]
        [InlineData("HAVA FİLTRESİ", ParcaTuru.HavaFiltresi)]
        [InlineData("YAKIT FİLTRESİ", ParcaTuru.YakitFiltresi)]
        public void AnahtarKelimeTablosuTuruBulur(string aciklama, ParcaTuru beklenen)
        {
            Assert.Equal(beklenen, ParcaEslestirici.Esle(aciklama));
        }

        [Theory]
        [InlineData("BİLİNMEYEN ÜRÜN")]
        [InlineData("")]
        [InlineData(null)]
        public void EslesmeyenDigerDoner(string aciklama)
        {
            Assert.Equal(ParcaTuru.Diger, ParcaEslestirici.Esle(aciklama));
        }

        [Theory]
        [InlineData("İŞÇİLİK")]
        [InlineData("iscilik bedeli")]
        [InlineData("KARGO")]
        [InlineData("kargo ucreti")]
        [InlineData("NAKLİYE")]
        public void IscilikVeKargoParcaSayilmaz(string aciklama)
        {
            Assert.True(ParcaEslestirici.AtlanmaliMi(aciklama));
        }

        [Theory]
        [InlineData("YAĞ FİLTRESİ")]
        [InlineData("BUJİ")]
        public void GercekParcaAtlanmaz(string aciklama)
        {
            Assert.False(ParcaEslestirici.AtlanmaliMi(aciklama));
        }

        [Fact]
        public void KalemListesiParcalaraCevrilirIscilikAtlanir()
        {
            var kalemler = new List<ReceiptItemResult>
            {
                new ReceiptItemResult { Ad = "MOTOR YAĞI 5W30", Tutar = 1800m },
                new ReceiptItemResult { Ad = "YAĞ FİLTRESİ", Tutar = 350m },
                new ReceiptItemResult { Ad = "İŞÇİLİK", Tutar = 500m },
                new ReceiptItemResult { Ad = "BİLİNMEYEN KALEM", Tutar = 120m }
            };

            var parcalar = ParcaEslestirici.Cevir(kalemler);

            Assert.Equal(3, parcalar.Count);
            Assert.Equal(ParcaTuru.MotorYagi, parcalar[0].ParcaTuru);
            Assert.Equal(1800m, parcalar[0].Tutar);
            Assert.Equal(1, parcalar[0].Adet);
            Assert.Equal(ParcaTuru.YagFiltresi, parcalar[1].ParcaTuru);
            Assert.Equal(ParcaTuru.Diger, parcalar[2].ParcaTuru);
            Assert.Equal("BİLİNMEYEN KALEM", parcalar[2].Aciklama);
        }

        [Fact]
        public void BosKalemListesiBosParcaListesiDoner()
        {
            Assert.Empty(ParcaEslestirici.Cevir(null));
            Assert.Empty(ParcaEslestirici.Cevir(new List<ReceiptItemResult>()));
        }
    }
}
