using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class UstaOzetTekilIndeksTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        public void Dispose()
        {
            _db.Dispose();
        }

        private static UstaCozumOzeti Ozet(string motor) => new UstaCozumOzeti
        {
            Marka = "Fiat",
            Model = "Egea",
            Motor = motor,
            BelirtiKategori = "fren",
            ParcaTuru = "balata",
            Sayi = 1,
            GuncellemeTarihi = DateTime.UtcNow
        };

        [Fact]
        public async Task AyniDogalAnahtarIkinciKezYazilamaz()
        {
            _db.Context.UstaCozumOzetleri.Add(Ozet("1.6 Dizel"));
            await _db.Context.SaveChangesAsync();

            _db.Context.UstaCozumOzetleri.Add(Ozet("1.6 Dizel"));

            await Assert.ThrowsAnyAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
        }

        [Fact]
        public async Task MotoruBosOlanDaTekildir()
        {
            _db.Context.UstaCozumOzetleri.Add(Ozet(string.Empty));
            await _db.Context.SaveChangesAsync();

            _db.Context.UstaCozumOzetleri.Add(Ozet(string.Empty));

            await Assert.ThrowsAnyAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
        }

        [Fact]
        public async Task FarkliMotorAyriSatirdir()
        {
            _db.Context.UstaCozumOzetleri.Add(Ozet("1.6 Dizel"));
            _db.Context.UstaCozumOzetleri.Add(Ozet("1.4 Benzin"));

            await _db.Context.SaveChangesAsync();

            Assert.Equal(2, await _db.Context.UstaCozumOzetleri.CountAsync());
        }
    }
}
