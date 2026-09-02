using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class LastikDegismezTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        public void Dispose()
        {
            _db.Dispose();
        }

        private int _sahipId;

        private Vehicle AracEkle()
        {
            if (_sahipId == 0)
            {
                var sahip = new AppUser
                {
                    CompanyId = _db.CompanyId,
                    Role = CompanyRole.Owner,
                    IsActive = true,
                    EmailDogrulandi = true,
                    Email = "lastik-sahip@garajim.local",
                    FullName = "Lastik Sahibi",
                    PasswordHash = new byte[] { 1 },
                    PasswordSalt = new byte[] { 1 },
                    CreatedAt = DateTime.UtcNow
                };
                _db.Context.Users.Add(sahip);
                _db.Context.SaveChanges();
                _sahipId = sahip.Id;
            }

            var arac = new Vehicle
            {
                CompanyId = _db.CompanyId,
                UserId = _sahipId,
                Plate = "34LST" + Guid.NewGuid().ToString("N").Substring(0, 3).ToUpperInvariant(),
                Brand = "Renault",
                Model = "Clio",
                Year = 2019,
                CurrentKm = 100000,
                FuelType = FuelType.Benzin,
                CreatedAt = DateTime.UtcNow
            };

            _db.Context.Vehicles.Add(arac);
            _db.Context.SaveChanges();
            return arac;
        }

        private LastikSeti Set(int vehicleId, string ad, bool takili)
        {
            return new LastikSeti
            {
                CompanyId = _db.CompanyId,
                VehicleId = vehicleId,
                Ad = ad,
                Mevsim = LastikMevsimi.Yaz,
                TakilmaTarihi = DateTime.UtcNow.Date.AddDays(-30),
                TakilmaKm = 90000,
                SokulmeTarihi = takili ? null : DateTime.UtcNow.Date.AddDays(-10),
                SokulmeKm = takili ? null : 95000,
                ToplamKm = takili ? 0 : 5000,
                Takili = takili,
                OlusturmaTarihi = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task AracaIkinciTakiliSetEklenemez()
        {
            var arac = AracEkle();

            _db.Context.LastikSetleri.Add(Set(arac.Id, "Birinci", true));
            await _db.Context.SaveChangesAsync();

            _db.Context.LastikSetleri.Add(Set(arac.Id, "İkinci", true));

            await Assert.ThrowsAnyAsync<DbUpdateException>(() => _db.Context.SaveChangesAsync());
        }

        [Fact]
        public async Task SokulmusSetlerSinirlamayaTakilmaz()
        {
            var arac = AracEkle();

            _db.Context.LastikSetleri.Add(Set(arac.Id, "Eski 1", false));
            _db.Context.LastikSetleri.Add(Set(arac.Id, "Eski 2", false));
            _db.Context.LastikSetleri.Add(Set(arac.Id, "Takılı", true));

            await _db.Context.SaveChangesAsync();

            var setler = await _db.Context.LastikSetleri.IgnoreQueryFilters().ToListAsync();

            Assert.Equal(3, setler.Count);
            Assert.Single(setler.Where(s => s.Takili));
        }

        [Fact]
        public async Task FarkliAraclarAyriAyriTakiliSetTutabilir()
        {
            var birinci = AracEkle();
            var ikinci = AracEkle();

            _db.Context.LastikSetleri.Add(Set(birinci.Id, "Birinci araç", true));
            _db.Context.LastikSetleri.Add(Set(ikinci.Id, "İkinci araç", true));

            await _db.Context.SaveChangesAsync();

            Assert.Equal(2, await _db.Context.LastikSetleri.IgnoreQueryFilters().CountAsync(s => s.Takili));
        }
    }
}
