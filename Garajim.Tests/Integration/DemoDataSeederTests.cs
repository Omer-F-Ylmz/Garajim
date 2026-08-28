using Garajim.Business.Concrete;
using Garajim.Business.Seed;
using Garajim.Core.Utilities.Security;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class DemoDataSeederTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private DemoDataSeeder CreateSeeder()
        {
            return new DemoDataSeeder(_db.CompanyDal, _db.UserDal, _db.VehicleDal, _db.MaintenanceDal, _db.FuelDal, _db.ExpenseDal, _db.ReminderDal, _db.AssignmentDal, _db.Tenant);
        }

        [Fact]
        public async Task IlkCalismadaDemoVerisiOlusturulur()
        {
            var eklendi = await CreateSeeder().RunAsync();

            var kullanici = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoEmail);
            var arac = await _db.Context.Vehicles.IgnoreQueryFilters().AsNoTracking().SingleAsync(v => v.UserId == kullanici.Id);

            Assert.True(eklendi);
            Assert.Equal("Demo Kullanıcı", kullanici.FullName);
            Assert.Equal(DemoDataSeeder.DemoPlate, arac.Plate);
            Assert.Equal(2, await _db.Context.MaintenanceRecords.IgnoreQueryFilters().CountAsync(m => m.VehicleId == arac.Id));
            Assert.Equal(3, await _db.Context.FuelRecords.IgnoreQueryFilters().CountAsync(f => f.VehicleId == arac.Id));
            Assert.Equal(2, await _db.Context.ExpenseRecords.IgnoreQueryFilters().CountAsync(e => e.VehicleId == arac.Id));
            Assert.Equal(1, await _db.Context.Reminders.IgnoreQueryFilters().CountAsync(r => r.VehicleId == arac.Id));
        }

        [Fact]
        public async Task IkinciCalismaVeriEklemez()
        {
            await CreateSeeder().RunAsync();
            var ikinciSonuc = await CreateSeeder().RunAsync();
            var ucuncuSonuc = await CreateSeeder().RunAsync();

            Assert.False(ikinciSonuc);
            Assert.False(ucuncuSonuc);
            Assert.Equal(2, await _db.Context.Users.IgnoreQueryFilters().CountAsync());
            Assert.Equal(1, await _db.Context.VehicleAssignments.IgnoreQueryFilters().CountAsync());
            Assert.Equal(1, await _db.Context.Vehicles.IgnoreQueryFilters().CountAsync());
            Assert.Equal(2, await _db.Context.MaintenanceRecords.IgnoreQueryFilters().CountAsync());
            Assert.Equal(3, await _db.Context.FuelRecords.IgnoreQueryFilters().CountAsync());
            Assert.Equal(2, await _db.Context.ExpenseRecords.IgnoreQueryFilters().CountAsync());
            Assert.Equal(1, await _db.Context.Reminders.IgnoreQueryFilters().CountAsync());
        }

        [Fact]
        public async Task DemoEkibiSahipVeSurucudenOlusur()
        {
            await CreateSeeder().RunAsync();

            var sahip = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoEmail);
            var surucu = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoDriverEmail);

            Assert.Equal(CompanyRole.Owner, sahip.Role);
            Assert.Equal(CompanyRole.Driver, surucu.Role);
            Assert.True(sahip.IsActive);
            Assert.True(surucu.IsActive);
            Assert.Equal(sahip.CompanyId, surucu.CompanyId);
        }

        [Fact]
        public async Task DemoAraciSurucuyeZimmetliGelir()
        {
            await CreateSeeder().RunAsync();

            var surucu = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoDriverEmail);
            var arac = await _db.Context.Vehicles.IgnoreQueryFilters().AsNoTracking().SingleAsync();
            var zimmet = await _db.Context.VehicleAssignments.IgnoreQueryFilters().AsNoTracking().SingleAsync();

            Assert.Equal(arac.Id, zimmet.VehicleId);
            Assert.Equal(surucu.Id, zimmet.UserId);
            Assert.Equal(arac.CompanyId, zimmet.CompanyId);
            Assert.Null(zimmet.EndDate);
        }

        [Fact]
        public async Task DemoSurucusuYalnizZimmetliAraciGorur()
        {
            await CreateSeeder().RunAsync();
            var surucu = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoDriverEmail);
            _db.Tenant.SetCompany(surucu.CompanyId);

            var araclar = await _db.VehicleAccess.GetAccessibleListAsync(surucu.Id);

            Assert.Single(araclar);
            Assert.Equal(DemoDataSeeder.DemoPlate, araclar[0].Plate);
        }

        [Fact]
        public async Task DemoKullanicisiBelgelenenSifreyleGirisYapabilir()
        {
            await CreateSeeder().RunAsync();
            var kullanici = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoEmail);

            var dogru = HashingHelper.VerifyPasswordHash(DemoDataSeeder.DemoPassword, kullanici.PasswordHash, kullanici.PasswordSalt);
            var yanlis = HashingHelper.VerifyPasswordHash("baska-sifre", kullanici.PasswordHash, kullanici.PasswordSalt);

            Assert.True(dogru);
            Assert.False(yanlis);
        }

        [Fact]
        public async Task DemoHatirlatmasiYaklasanlarListesindeGorunur()
        {
            await CreateSeeder().RunAsync();
            var kullanici = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoEmail);
            _db.Tenant.SetCompany(kullanici.CompanyId);
            var manager = new ReminderManager(_db.ReminderDal, _db.VehicleAccess);

            var yaklasan = await manager.GetUpcomingAsync(kullanici.Id, 30);

            Assert.True(yaklasan.Success);
            Assert.Single(yaklasan.Data);
            Assert.Equal(DemoDataSeeder.DemoPlate, yaklasan.Data[0].Plate);
        }

        [Fact]
        public async Task DemoVerisiRaporlariBesler()
        {
            await CreateSeeder().RunAsync();
            var kullanici = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoEmail);
            var arac = await _db.Context.Vehicles.IgnoreQueryFilters().AsNoTracking().SingleAsync();
            _db.Tenant.SetCompany(kullanici.CompanyId);
            var manager = new ReportManager(_db.VehicleAccess, _db.MaintenanceDal, _db.FuelDal, _db.ExpenseDal);

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
