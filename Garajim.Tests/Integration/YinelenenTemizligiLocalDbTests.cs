using Garajim.Core.Multitenancy;
using Garajim.Dal.Concrete.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Garajim.Tests.Integration
{
    public class YinelenenTemizligiLocalDbTests
    {
        private const string SonOncekiMigration = "20260908074822_GirisKilidi";

        private static string BaglantiDizesi(string veritabani) =>
            "Server=(localdb)\\MSSQLLocalDB;Database=" + veritabani
            + ";Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

        private static bool Istendi()
        {
            return string.Equals(Environment.GetEnvironmentVariable("GARAJIM_LOCALDB_TEST"), "1",
                StringComparison.Ordinal);
        }

        private static bool LocalDbVar()
        {
            try
            {
                using var baglanti = new SqlConnection(BaglantiDizesi("master"));
                baglanti.Open();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static GarajimDbContext Baglam(string veritabani)
        {
            var secenekler = new DbContextOptionsBuilder<GarajimDbContext>()
                .UseSqlServer(BaglantiDizesi(veritabani))
                .Options;

            return new GarajimDbContext(secenekler, new TenantContext());
        }

        [Fact]
        public void YinelenenVeriyleIkiMigrationSqlServerdaGecer()
        {
            if (!Istendi() || !LocalDbVar())
            {
                return;
            }

            var veritabani = "GarajimYinelenen_" + Guid.NewGuid().ToString("N").Substring(0, 12);

            using var baglam = Baglam(veritabani);

            try
            {
                baglam.Database.GetService<IMigrator>().Migrate(SonOncekiMigration);

                baglam.Database.ExecuteSqlRaw(@"
INSERT INTO UstaCozumOzetleri (Marka, Model, Motor, BelirtiKategori, ParcaTuru, Sayi, GuncellemeTarihi)
VALUES ('Fiat', 'Egea', '1.6 Dizel', 'fren', 'balata', 5, '2026-01-01'),
       ('Fiat', 'Egea', '1.6 Dizel', 'fren', 'balata', 7, '2026-03-01'),
       ('Fiat', 'Egea', '1.6 Dizel', 'fren', 'balata', 2, '2026-02-01'),
       ('Fiat', 'Egea', NULL, 'fren', 'balata', 4, '2026-01-01'),
       ('Fiat', 'Egea', NULL, 'fren', 'balata', 6, '2026-01-02'),
       ('Opel', 'Astra', '1.4 Benzin', 'fren', 'balata', 3, '2026-01-01');");

                baglam.Database.ExecuteSqlRaw("ALTER TABLE HasarFotograflari NOCHECK CONSTRAINT ALL;");
                baglam.Database.ExecuteSqlRaw(@"
INSERT INTO HasarFotograflari (CompanyId, HasarDosyasiId, DocumentId, Etiket, Sira, OlusturmaTarihi)
VALUES (1, 10, 1, 0, 1, '2026-09-08'),
       (1, 10, 2, 0, 1, '2026-09-08'),
       (1, 10, 3, 0, 3, '2026-09-08'),
       (1, 11, 4, 0, 2, '2026-09-08'),
       (1, 11, 5, 0, 2, '2026-09-08'),
       (1, 12, 6, 0, 1, '2026-09-08');");

                baglam.Database.GetService<IMigrator>().Migrate();

                var ozetler = baglam.UstaCozumOzetleri.AsNoTracking().OrderBy(o => o.Id).ToList();

                Assert.Equal(3, ozetler.Count);
                Assert.Equal(14, ozetler[0].Sayi);
                Assert.Equal(new DateTime(2026, 3, 1), ozetler[0].GuncellemeTarihi);
                Assert.Equal(10, ozetler[1].Sayi);
                Assert.Equal(3, ozetler[2].Sayi);

                var fotolar = baglam.HasarFotograflari.IgnoreQueryFilters().AsNoTracking().OrderBy(f => f.Id).ToList();

                Assert.Equal(6, fotolar.Count);
                Assert.Equal(new[] { 1, 2, 3, 1, 2, 1 }, fotolar.Select(f => f.Sira).ToArray());

                Assert.Equal(0, baglam.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS Value FROM sys.tables WHERE name = 'HasarFotoSiraGecici'").Single());
            }
            finally
            {
                baglam.Database.EnsureDeleted();
            }
        }
    }
}
