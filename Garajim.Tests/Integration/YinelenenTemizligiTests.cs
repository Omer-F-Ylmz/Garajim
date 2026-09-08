using Garajim.Dal.Sorgular;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class YinelenenTemizligiTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        public YinelenenTemizligiTests()
        {
            _db.Context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");
            _db.Context.Database.ExecuteSqlRaw("DROP INDEX \"UX_UstaCozumOzeti_DogalAnahtar\";");
            _db.Context.Database.ExecuteSqlRaw("DROP INDEX \"UX_HasarFoto_DosyaSira\";");
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        private void OzetYaz(int id, string motor, int sayi, string tarih)
        {
            var motorDegeri = motor == null ? "NULL" : "'" + motor + "'";

            _db.Context.Database.ExecuteSqlRaw(
                "INSERT INTO UstaCozumOzetleri (Id, Marka, Model, Motor, BelirtiKategori, ParcaTuru, Sayi, GuncellemeTarihi) "
                + "VALUES (" + id + ", 'Fiat', 'Egea', " + motorDegeri + ", 'fren', 'balata', " + sayi + ", '" + tarih + "');");
        }

        private void FotoYaz(int id, int dosyaId, int sira)
        {
            _db.Context.Database.ExecuteSqlRaw(
                "INSERT INTO HasarFotograflari (Id, CompanyId, HasarDosyasiId, DocumentId, Etiket, Sira, OlusturmaTarihi) "
                + "VALUES (" + id + ", " + _db.CompanyId + ", " + dosyaId + ", " + id + ", 0, " + sira + ", '2026-09-08 00:00:00');");
        }

        private void FotoTemizle()
        {
            _db.Context.Database.ExecuteSqlRaw(YinelenenTemizligi.HasarFotoSiraGeciciOlustur);
            _db.Context.Database.ExecuteSqlRaw(YinelenenTemizligi.HasarFotoSiraGeciciDoldur);
            _db.Context.Database.ExecuteSqlRaw(YinelenenTemizligi.HasarFotoSiraUygula);
            _db.Context.Database.ExecuteSqlRaw(YinelenenTemizligi.HasarFotoSiraGeciciSil);
        }

        private void OzetTemizle()
        {
            _db.Context.Database.ExecuteSqlRaw(YinelenenTemizligi.UstaOzetiTopla);
            _db.Context.Database.ExecuteSqlRaw(YinelenenTemizligi.UstaOzetiKopyalariSil);
        }

        private void IndeksKur(string ad, string tablo, string kolonlar)
        {
            _db.Context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX \"" + ad + "\" ON " + tablo + " (" + kolonlar + ");");
        }

        [Fact]
        public void UstaOzetiYinelenenleriToplarVeTekSatiraIndirir()
        {
            OzetYaz(1, "1.6 Dizel", 5, "2026-01-01 00:00:00");
            OzetYaz(2, "1.6 Dizel", 7, "2026-03-01 00:00:00");
            OzetYaz(3, "1.6 Dizel", 2, "2026-02-01 00:00:00");

            OzetTemizle();

            var kalan = _db.Context.UstaCozumOzetleri.AsNoTracking().ToList();

            Assert.Single(kalan);
            Assert.Equal(1, kalan[0].Id);
            Assert.Equal(14, kalan[0].Sayi);
            Assert.Equal(new DateTime(2026, 3, 1), kalan[0].GuncellemeTarihi);
        }

        [Fact]
        public void UstaOzetiMotoruBosOlanYinelenenleriDeToplar()
        {
            OzetYaz(1, null, 4, "2026-01-01 00:00:00");
            OzetYaz(2, null, 6, "2026-01-02 00:00:00");

            OzetTemizle();

            var kalan = _db.Context.UstaCozumOzetleri.AsNoTracking().Single();

            Assert.Equal(10, kalan.Sayi);
        }

        [Fact]
        public void UstaOzetiFarkliAnahtarlariBirlestirmez()
        {
            OzetYaz(1, "1.6 Dizel", 5, "2026-01-01 00:00:00");
            OzetYaz(2, "1.4 Benzin", 3, "2026-01-01 00:00:00");
            OzetYaz(3, "1.6 Dizel", 4, "2026-01-01 00:00:00");

            OzetTemizle();

            var kalan = _db.Context.UstaCozumOzetleri.AsNoTracking().OrderBy(o => o.Id).ToList();

            Assert.Equal(2, kalan.Count);
            Assert.Equal(9, kalan[0].Sayi);
            Assert.Equal(3, kalan[1].Sayi);
        }

        [Fact]
        public void UstaOzetiTemizlikSonrasiTekilIndeksKurulabilir()
        {
            OzetYaz(1, "1.6 Dizel", 5, "2026-01-01 00:00:00");
            OzetYaz(2, "1.6 Dizel", 7, "2026-01-01 00:00:00");
            OzetYaz(3, null, 1, "2026-01-01 00:00:00");

            OzetTemizle();

            IndeksKur("UX_UstaCozumOzeti_DogalAnahtar", "UstaCozumOzetleri",
                "Marka, Model, Motor, BelirtiKategori, ParcaTuru");
        }

        [Fact]
        public void UstaOzetiTemizlikFikirSabitidir()
        {
            OzetYaz(1, "1.6 Dizel", 5, "2026-01-01 00:00:00");
            OzetYaz(2, "1.6 Dizel", 7, "2026-01-01 00:00:00");

            OzetTemizle();
            OzetTemizle();

            var kalan = _db.Context.UstaCozumOzetleri.AsNoTracking().Single();

            Assert.Equal(12, kalan.Sayi);
        }

        [Fact]
        public void HasarFotoSirasiYinelenenDosyadaYenidenNumaralanir()
        {
            FotoYaz(1, 10, 1);
            FotoYaz(2, 10, 1);
            FotoYaz(3, 10, 3);

            FotoTemizle();

            var kalan = _db.Context.HasarFotograflari.AsNoTracking().OrderBy(f => f.Id).ToList();

            Assert.Equal(3, kalan.Count);
            Assert.Equal(new[] { 1, 2, 3 }, kalan.Select(f => f.Sira).ToArray());
        }

        [Fact]
        public void HasarFotoTemizligiSatirSilmez()
        {
            FotoYaz(1, 10, 1);
            FotoYaz(2, 10, 1);
            FotoYaz(3, 11, 1);

            FotoTemizle();

            Assert.Equal(3, _db.Context.HasarFotograflari.AsNoTracking().Count());
            Assert.Equal(1, _db.Context.HasarFotograflari.AsNoTracking().Single(f => f.Id == 3).Sira);
        }

        [Fact]
        public void HasarFotoTemizligiSonrasiTekilIndeksKurulabilir()
        {
            FotoYaz(1, 10, 1);
            FotoYaz(2, 10, 1);
            FotoYaz(3, 10, 1);
            FotoYaz(4, 11, 2);
            FotoYaz(5, 11, 2);

            FotoTemizle();

            IndeksKur("UX_HasarFoto_DosyaSira", "HasarFotograflari", "HasarDosyasiId, Sira");
        }

        [Fact]
        public void HasarFotoTemizligiFikirSabitidir()
        {
            FotoYaz(1, 10, 1);
            FotoYaz(2, 10, 1);

            FotoTemizle();
            var ilk = _db.Context.HasarFotograflari.AsNoTracking().OrderBy(f => f.Id).Select(f => f.Sira).ToArray();

            FotoTemizle();
            var ikinci = _db.Context.HasarFotograflari.AsNoTracking().OrderBy(f => f.Id).Select(f => f.Sira).ToArray();

            Assert.Equal(ilk, ikinci);
        }
    }
}
