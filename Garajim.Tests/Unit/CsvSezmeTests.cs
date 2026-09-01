using System.Text;
using Garajim.Business.Concrete.Import;

namespace Garajim.Tests.Unit
{
    public class CsvSezmeTests
    {
        private static byte[] Bayt(string metin, Encoding kodlama = null)
        {
            return (kodlama ?? new UTF8Encoding(true)).GetBytes(metin);
        }

        [Theory]
        [InlineData("a;b;c\n1;2;3", ';')]
        [InlineData("a,b,c\n1,2,3", ',')]
        [InlineData("a\tb\tc\n1\t2\t3", '\t')]
        public void AyracSezilir(string icerik, char beklenen)
        {
            var tablo = CsvOkuyucu.Oku(Bayt(icerik));

            Assert.Equal(beklenen, tablo.Ayrac);
        }

        [Fact]
        public void NoktaliVirgulAgirlikliSatirdaVirgulOndalikKarismaz()
        {
            var tablo = CsvOkuyucu.Oku(Bayt("tarih;tutar;km\n01.02.2026;1.484,36;123456"));

            Assert.Equal(';', tablo.Ayrac);
            Assert.Single(tablo.Satirlar);
            Assert.Equal("1.484,36", tablo.Satirlar[0][1]);
        }

        [Fact]
        public void Utf8BomTemizlenir()
        {
            var tablo = CsvOkuyucu.Oku(Bayt("dosya;tur\na;b"));

            Assert.Equal("dosya", tablo.Basliklar[0]);
        }

        [Fact]
        public void Windows1254OkunabilirTurkceKarakterKorunur()
        {
            var kodlama = CodePagesEncodingProvider.Instance.GetEncoding(1254);
            var tablo = CsvOkuyucu.Oku(kodlama.GetBytes("açıklama;tutar\nyağ değişimi;100"));

            Assert.Equal("açıklama", tablo.Basliklar[0]);
            Assert.Equal("yağ değişimi", tablo.Satirlar[0][0]);
        }

        [Fact]
        public void TirnakliAlanIcindekiAyracBolmez()
        {
            var tablo = CsvOkuyucu.Oku(Bayt("a;b\n\"yag; filtre\";100"));

            Assert.Equal("yag; filtre", tablo.Satirlar[0][0]);
        }

        [Theory]
        [InlineData("01.02.2026", 2026, 2, 1)]
        [InlineData("2026-02-01", 2026, 2, 1)]
        [InlineData("01/02/2026", 2026, 2, 1)]
        [InlineData("2026-02-01 14:30", 2026, 2, 1)]
        public void TarihIkiBicimdenDeOkunur(string metin, int yil, int ay, int gun)
        {
            Assert.Equal(new DateTime(yil, ay, gun), CsvDeger.Tarih(metin));
        }

        [Fact]
        public void GecersizTarihNullDoner()
        {
            Assert.Null(CsvDeger.Tarih("saçma"));
            Assert.Null(CsvDeger.Tarih(""));
        }

        [Theory]
        [InlineData("1.484,36", 1484.36)]
        [InlineData("1,484.36", 1484.36)]
        [InlineData("1484.36", 1484.36)]
        [InlineData("1484,36", 1484.36)]
        [InlineData("100", 100)]
        public void SayiTrVeEnBicimdenOkunur(string metin, double beklenen)
        {
            Assert.Equal((decimal)beklenen, CsvDeger.Sayi(metin).Value);
        }

        [Fact]
        public void GecersizSayiNullDoner()
        {
            Assert.Null(CsvDeger.Sayi("abc"));
            Assert.Null(CsvDeger.Sayi(""));
        }

        [Fact]
        public void SatirHashiAyniSatirIcinAyniFarkliSatirIcinFarklidir()
        {
            var birinci = ImportSatirHash.Hesapla(7, new[] { "01.02.2026", "1.484,36", "123456" });
            var ayni = ImportSatirHash.Hesapla(7, new[] { "01.02.2026", "1.484,36", "123456" });
            var farkliArac = ImportSatirHash.Hesapla(8, new[] { "01.02.2026", "1.484,36", "123456" });
            var farkliSatir = ImportSatirHash.Hesapla(7, new[] { "01.02.2026", "1.484,37", "123456" });

            Assert.Equal(birinci, ayni);
            Assert.NotEqual(birinci, farkliArac);
            Assert.NotEqual(birinci, farkliSatir);
            Assert.Equal(64, birinci.Length);
        }

        [Fact]
        public void FuelioBolumBasliklariAyristirilir()
        {
            var icerik = @"## Vehicle\nName;Fuel unit\nClio;L\n\n## Log\nData;Odometer (km);Fuel volume;Full;Price;Total cost\n01.02.2026;123456;42,5;1;46,60;1980,50\n\n## Costs\nData;Odometer (km);Cost title;Cost;Notes\n05.02.2026;123500;Otopark;350;aylik\n";
            var sablon = ImportSablonlari.Sez(CsvOkuyucu.Oku(Bayt(icerik)));

            Assert.Equal("Fuelio", sablon);
        }

        [Fact]
        public void DrivvoTurkceSutunlariEslesir()
        {
            var tablo = CsvOkuyucu.Oku(Bayt("Tarih;Kilometre;Litre;Toplam maliyet;Fiyat\n01.02.2026;123456;42,5;1980,50;46,60"));
            var eslesme = ImportSablonlari.SutunOner(tablo, "Yakit");

            Assert.Equal(0, eslesme["tarih"]);
            Assert.Equal(1, eslesme["km"]);
            Assert.Equal(2, eslesme["litre"]);
            Assert.Equal(3, eslesme["tutar"]);
        }

        [Fact]
        public void DrivvoIngilizceSutunlariDaEslesir()
        {
            var tablo = CsvOkuyucu.Oku(Bayt("Date;Odometer;Volume;Total cost\n2026-02-01;123456;42.5;1980.50"));
            var eslesme = ImportSablonlari.SutunOner(tablo, "Yakit");

            Assert.Equal(0, eslesme["tarih"]);
            Assert.Equal(1, eslesme["km"]);
            Assert.Equal(2, eslesme["litre"]);
            Assert.Equal(3, eslesme["tutar"]);
        }

        [Fact]
        public void FuelioYakitIcinLogBolumuSecilir()
        {
            var icerik = @"## Vehicle
Name;Fuel unit
Clio;L

## Log
Data;Odometer (km);Fuel volume;Total cost
01.02.2026;123456;42,5;1980,50

## Costs
Data;Odometer (km);Cost title;Cost
05.02.2026;123500;Otopark;350
";
            var tablo = CsvOkuyucu.Oku(Bayt(icerik), "Yakit");

            Assert.Equal("Log", tablo.Bolum);
            Assert.Single(tablo.Satirlar);
            Assert.Equal("42,5", tablo.Satirlar[0][2]);
            Assert.Equal(7, tablo.SatirNolari[0]);
        }

        [Fact]
        public void FuelioMasrafIcinCostsBolumuSecilir()
        {
            var icerik = @"## Vehicle
Name;Fuel unit
Clio;L

## Log
Data;Odometer (km);Fuel volume;Total cost
01.02.2026;123456;42,5;1980,50

## Costs
Data;Odometer (km);Cost title;Cost
05.02.2026;123500;Otopark;350
";
            var tablo = CsvOkuyucu.Oku(Bayt(icerik), "Masraf");

            Assert.Equal("Costs", tablo.Bolum);
            Assert.Single(tablo.Satirlar);
            Assert.Equal("Otopark", tablo.Satirlar[0][2]);
        }

        [Fact]
        public void BolumsuzDosyadaSatirNumaralariDosyaylaAyniKalir()
        {
            var tablo = CsvOkuyucu.Oku(Bayt(@"a;b
1;2
3;4"));

            Assert.Equal(new[] { 2, 3 }, tablo.SatirNolari);
        }

        [Fact]
        public void BilinmeyenSutunEslesmedenBirakilir()
        {
            var tablo = CsvOkuyucu.Oku(Bayt("Kolon1;Kolon2\na;b"));
            var eslesme = ImportSablonlari.SutunOner(tablo, "Yakit");

            Assert.False(eslesme.ContainsKey("tarih"));
            Assert.False(eslesme.ContainsKey("tutar"));
        }
    }
}
