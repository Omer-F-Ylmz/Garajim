using Garajim.Business.Concrete;
using Garajim.Business.Seed;
using Garajim.Core.Utilities.Security;
using Garajim.Entity.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class DemoDataSeederTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private DemoDataSeeder CreateSeeder()
        {
            return new DemoDataSeeder(_db.UserDal, _db.VehicleDal, _db.MaintenanceDal, _db.FuelDal, _db.ExpenseDal, _db.ReminderDal);
        }

        [Fact]
        public async Task IlkCalismadaDemoVerisiOlusturulur()
        {
            var eklendi = await CreateSeeder().RunAsync();

            var kullanici = await _db.Context.Users.AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoEmail);
            var arac = await _db.Context.Vehicles.AsNoTracking().SingleAsync(v => v.UserId == kullanici.Id);

            Assert.True(eklendi);
            Assert.Equal("Demo Kullanıcı", kullanici.FullName);
            Assert.Equal(DemoDataSeeder.DemoPlate, arac.Plate);
            Assert.Equal(2, await _db.Context.MaintenanceRecords.CountAsync(m => m.VehicleId == arac.Id));
            Assert.Equal(3, await _db.Context.FuelRecords.CountAsync(f => f.VehicleId == arac.Id));
            Assert.Equal(2, await _db.Context.ExpenseRecords.CountAsync(e => e.VehicleId == arac.Id));
            Assert.Equal(1, await _db.Context.Reminders.CountAsync(r => r.VehicleId == arac.Id));
        }

        [Fact]
        public async Task IkinciCalismaVeriEklemez()
        {
            await CreateSeeder().RunAsync();
            var ikinciSonuc = await CreateSeeder().RunAsync();
            var ucuncuSonuc = await CreateSeeder().RunAsync();

            Assert.False(ikinciSonuc);
            Assert.False(ucuncuSonuc);
            Assert.Equal(1, await _db.Context.Users.CountAsync());
            Assert.Equal(1, await _db.Context.Vehicles.CountAsync());
            Assert.Equal(2, await _db.Context.MaintenanceRecords.CountAsync());
            Assert.Equal(3, await _db.Context.FuelRecords.CountAsync());
            Assert.Equal(2, await _db.Context.ExpenseRecords.CountAsync());
            Assert.Equal(1, await _db.Context.Reminders.CountAsync());
        }

        [Fact]
        public async Task DemoKullanicisiBelgelenenSifreyleGirisYapabilir()
        {
            await CreateSeeder().RunAsync();
            var kullanici = await _db.Context.Users.AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoEmail);

            var dogru = HashingHelper.VerifyPasswordHash(DemoDataSeeder.DemoPassword, kullanici.PasswordHash, kullanici.PasswordSalt);
            var yanlis = HashingHelper.VerifyPasswordHash("baska-sifre", kullanici.PasswordHash, kullanici.PasswordSalt);

            Assert.True(dogru);
            Assert.False(yanlis);
        }

        [Fact]
        public async Task DemoHatirlatmasiYaklasanlarListesindeGorunur()
        {
            await CreateSeeder().RunAsync();
            var kullanici = await _db.Context.Users.AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoEmail);
            var manager = new ReminderManager(_db.ReminderDal, _db.VehicleDal);

            var yaklasan = await manager.GetUpcomingAsync(kullanici.Id, 30);

            Assert.True(yaklasan.Success);
            Assert.Single(yaklasan.Data);
            Assert.Equal(DemoDataSeeder.DemoPlate, yaklasan.Data[0].Plate);
        }

        [Fact]
        public async Task DemoVerisiRaporlariBesler()
        {
            await CreateSeeder().RunAsync();
            var kullanici = await _db.Context.Users.AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoEmail);
            var arac = await _db.Context.Vehicles.AsNoTracking().SingleAsync();
            var manager = new ReportManager(_db.VehicleDal, _db.MaintenanceDal, _db.FuelDal, _db.ExpenseDal);

            var ozet = await manager.GetSummaryAsync(kullanici.Id, arac.Id, DateTime.UtcNow.Date.AddYears(-1), DateTime.UtcNow.Date);
            var yakit = await manager.GetFuelStatsAsync(kullanici.Id, arac.Id);

            Assert.True(ozet.Success);
            Assert.True(ozet.Data.GrandTotal > 0);
            Assert.True(yakit.Success);
            Assert.True(yakit.Data.AverageConsumptionPer100Km > 0);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
