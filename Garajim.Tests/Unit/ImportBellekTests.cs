using System.Text;
using Garajim.Business.Concrete;
using Garajim.Business.Concrete.Import;

namespace Garajim.Tests.Unit
{
    public class ImportBellekTests
    {
        private static byte[] CokSatirliCsv(int satir)
        {
            var sb = new StringBuilder();
            sb.Append("tarih;km;litre;tutar\n");

            for (var i = 0; i < satir; i++)
            {
                sb.Append("01.01.2026;1;1;1\n");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        [Fact]
        public void OkuyucuSatirUstSinirindaDurur()
        {
            var icerik = CokSatirliCsv(ImportManager.MaxSatir * 4);

            var tablo = CsvOkuyucu.Oku(icerik, "Yakit");

            Assert.True(tablo.Satirlar.Count <= ImportManager.MaxSatir + 1,
                "Okuyucu üst sınırın çok üstünde satır tuttu: " + tablo.Satirlar.Count);
            Assert.True(tablo.SinirAsildi, "Üst sınır aşıldığı hâlde bayrak set edilmedi.");
        }

        [Fact]
        public void SinirAltindakiDosyaTamOkunur()
        {
            var icerik = CokSatirliCsv(50);

            var tablo = CsvOkuyucu.Oku(icerik, "Yakit");

            Assert.Equal(50, tablo.Satirlar.Count);
            Assert.False(tablo.SinirAsildi);
        }

        [Fact]
        public void HamSatirlarSinirlidir()
        {
            var icerik = CokSatirliCsv(ImportManager.MaxSatir * 4);

            var tablo = CsvOkuyucu.Oku(icerik, "Yakit");

            Assert.True(tablo.HamSatirlar.Count <= ImportManager.MaxSatir + 1,
                "HamSatirlar sınırsız büyüyor: " + tablo.HamSatirlar.Count);
        }
    }
}
