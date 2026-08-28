using Garajim.Business.Seed;
using Garajim.Core.Utilities.Security;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class DemoSeedIncrementalTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private DemoDataSeeder Seeder()
        {
            return new DemoDataSeeder(
                _db.CompanyDal,
                _db.UserDal,
                _db.VehicleDal,
                _db.MaintenanceDal,
                _db.FuelDal,
                _db.ExpenseDal,
                _db.ReminderDal,
                _db.AssignmentDal,
                _db.Tenant);
        }

        private (Company Sirket, AppUser Sahip, Vehicle Arac) YayindakiDurumuKur()
        {
            var sirket = new Company
            {
                Name = DemoDataSeeder.DemoCompanyName,
                PlanType = PlanType.Standart,
                CreatedAt = new DateTime(2026, 1, 1)
            };
            _db.Context.Companies.Add(sirket);
            _db.Context.SaveChanges();

            HashingHelper.CreatePasswordHash(DemoDataSeeder.DemoPassword, out var hash, out var salt);
            var sahip = new AppUser
            {
                CompanyId = sirket.Id,
                Role = CompanyRole.Owner,
                IsActive = true,
                Email = DemoDataSeeder.DemoEmail,
                FullName = "Demo Kullanıcı",
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = new DateTime(2026, 1, 1)
            };
            _db.Context.Users.Add(sahip);
            _db.Context.SaveChanges();

            var arac = new Vehicle
            {
                CompanyId = sirket.Id,
                UserId = sahip.Id,
                Plate = DemoDataSeeder.DemoPlate,
                Brand = "Renault",
                Model = "Clio",
                Year = 2018,
                CurrentKm = 122000,
                FuelType = FuelType.Benzin,
                CreatedAt = new DateTime(2026, 1, 1)
            };
            _db.Context.Vehicles.Add(arac);
            _db.Context.SaveChanges();

            return (sirket, sahip, arac);
        }

        private VehicleAssignment ZimmetEkle(Vehicle arac, int userId, DateTime baslangic)
        {
            var zimmet = new VehicleAssignment
            {
                CompanyId = arac.CompanyId,
                VehicleId = arac.Id,
                UserId = userId,
                StartDate = baslangic,
                EndDate = null,
                AssignedByUserId = userId,
                CreatedAt = baslangic
            };
            _db.Context.VehicleAssignments.Add(zimmet);
            _db.Context.SaveChanges();
            return zimmet;
        }

        private Task<AppUser> SurucuyuOkuAsync()
        {
            return _db.Context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == DemoDataSeeder.DemoDriverEmail);
        }

        [Fact]
        public async Task DoluVeritabaninaEksikSurucuEklenir()
        {
            var yayin = YayindakiDurumuKur();

            var degisti = await Seeder().RunAsync();

            var surucu = await SurucuyuOkuAsync();

            Assert.True(degisti);
            Assert.NotNull(surucu);
            Assert.Equal(CompanyRole.Driver, surucu.Role);
            Assert.True(surucu.IsActive);
            Assert.Equal(yayin.Sirket.Id, surucu.CompanyId);
            Assert.True(HashingHelper.VerifyPasswordHash(DemoDataSeeder.DemoDriverPassword, surucu.PasswordHash, surucu.PasswordSalt));
        }

        [Fact]
        public async Task AktifZimmetYokkenSurucuyeZimmetAcilir()
        {
            var yayin = YayindakiDurumuKur();

            await Seeder().RunAsync();

            var surucu = await SurucuyuOkuAsync();
            var zimmetler = await _db.Context.VehicleAssignments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(a => a.VehicleId == yayin.Arac.Id)
                .ToListAsync();

            var aktif = Assert.Single(zimmetler.Where(a => a.EndDate == null));
            Assert.Equal(surucu.Id, aktif.UserId);
            Assert.Equal(yayin.Arac.CompanyId, aktif.CompanyId);
        }

        [Fact]
        public async Task MevcutAktifZimmeteDokunulmaz()
        {
            var yayin = YayindakiDurumuKur();
            var sahibinZimmeti = ZimmetEkle(yayin.Arac, yayin.Sahip.Id, new DateTime(2026, 8, 28));

            await Seeder().RunAsync();

            var zimmetler = await _db.Context.VehicleAssignments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(a => a.VehicleId == yayin.Arac.Id)
                .ToListAsync();

            var tek = Assert.Single(zimmetler);
            Assert.Equal(sahibinZimmeti.Id, tek.Id);
            Assert.Equal(yayin.Sahip.Id, tek.UserId);
            Assert.Null(tek.EndDate);
            Assert.Equal(new DateTime(2026, 8, 28), tek.StartDate);
        }

        [Fact]
        public async Task SirketAdiFarkliOlsaBileIkinciDemoSirketiAcilmaz()
        {
            var yayin = YayindakiDurumuKur();
            var sirket = _db.Context.Companies.Single(c => c.Id == yayin.Sirket.Id);
            sirket.Name = "Yeniden Adlandırılmış Filo";
            _db.Context.SaveChanges();

            await Seeder().RunAsync();

            var sirketler = await _db.Context.Companies.AsNoTracking().ToListAsync();
            var surucu = await SurucuyuOkuAsync();
            var aktif = await _db.Context.VehicleAssignments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(a => a.EndDate == null);

            var tek = Assert.Single(sirketler.Where(c => c.Id == yayin.Sirket.Id));
            Assert.Equal("Yeniden Adlandırılmış Filo", tek.Name);
            Assert.DoesNotContain(sirketler, c => c.Name == DemoDataSeeder.DemoCompanyName);
            Assert.Equal(yayin.Sirket.Id, surucu.CompanyId);
            Assert.Equal(surucu.Id, aktif.UserId);
            Assert.Equal(yayin.Arac.Id, aktif.VehicleId);
        }

        [Fact]
        public async Task MevcutAracaAltKayitEklenmez()
        {
            var yayin = YayindakiDurumuKur();

            await Seeder().RunAsync();

            Assert.Equal(0, await _db.Context.MaintenanceRecords.IgnoreQueryFilters().CountAsync());
            Assert.Equal(0, await _db.Context.FuelRecords.IgnoreQueryFilters().CountAsync());
            Assert.Equal(0, await _db.Context.ExpenseRecords.IgnoreQueryFilters().CountAsync());
            Assert.Equal(0, await _db.Context.Reminders.IgnoreQueryFilters().CountAsync());
            Assert.Equal(1, await _db.Context.Vehicles.IgnoreQueryFilters().CountAsync(v => v.Id == yayin.Arac.Id));
        }

        [Fact]
        public async Task TamDoluVeritabanindaSeedHicbirSeyiDegistirmez()
        {
            await Seeder().RunAsync();

            var oncekiZimmet = await _db.Context.VehicleAssignments.IgnoreQueryFilters().AsNoTracking().SingleAsync();
            var oncekiSayimlar = await SayimlariAlAsync();

            var degisti = await Seeder().RunAsync();

            var sonrakiZimmet = await _db.Context.VehicleAssignments.IgnoreQueryFilters().AsNoTracking().SingleAsync();

            Assert.False(degisti);
            Assert.Equal(oncekiSayimlar, await SayimlariAlAsync());
            Assert.Equal(oncekiZimmet.Id, sonrakiZimmet.Id);
            Assert.Equal(oncekiZimmet.UserId, sonrakiZimmet.UserId);
            Assert.Equal(oncekiZimmet.StartDate, sonrakiZimmet.StartDate);
        }

        [Fact]
        public async Task DemoSirketiDisindakiVerilereDokunulmaz()
        {
            var yabanciSirket = _db.SirketEkle("Yabancı Filo");
            var yabanciKullanici = _db.KullaniciEkle("yabanci@garajim.local", yabanciSirket.Id);
            var yabanciArac = _db.AracEkleSirketle(yabanciKullanici.Id, "34YBN111", yabanciSirket.Id);
            var yabanciZimmet = ZimmetEkle(yabanciArac, yabanciKullanici.Id, new DateTime(2026, 5, 1));

            await Seeder().RunAsync();

            var arac = await _db.Context.Vehicles.IgnoreQueryFilters().AsNoTracking().SingleAsync(v => v.Id == yabanciArac.Id);
            var zimmet = await _db.Context.VehicleAssignments.IgnoreQueryFilters().AsNoTracking().SingleAsync(a => a.Id == yabanciZimmet.Id);
            var kullanici = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Id == yabanciKullanici.Id);

            Assert.Equal(yabanciSirket.Id, arac.CompanyId);
            Assert.Equal(yabanciKullanici.Id, zimmet.UserId);
            Assert.Null(zimmet.EndDate);
            Assert.Equal(CompanyRole.Owner, kullanici.Role);
        }

        private async Task<string> SayimlariAlAsync()
        {
            var sirket = await _db.Context.Companies.AsNoTracking().CountAsync();
            var kullanici = await _db.Context.Users.IgnoreQueryFilters().CountAsync();
            var arac = await _db.Context.Vehicles.IgnoreQueryFilters().CountAsync();
            var zimmet = await _db.Context.VehicleAssignments.IgnoreQueryFilters().CountAsync();
            var bakim = await _db.Context.MaintenanceRecords.IgnoreQueryFilters().CountAsync();
            var yakit = await _db.Context.FuelRecords.IgnoreQueryFilters().CountAsync();
            var masraf = await _db.Context.ExpenseRecords.IgnoreQueryFilters().CountAsync();
            var hatirlatma = await _db.Context.Reminders.IgnoreQueryFilters().CountAsync();

            return $"{sirket}|{kullanici}|{arac}|{zimmet}|{bakim}|{yakit}|{masraf}|{hatirlatma}";
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
