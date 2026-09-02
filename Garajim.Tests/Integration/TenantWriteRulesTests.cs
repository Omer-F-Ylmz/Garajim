using Garajim.Business.Concrete;
using Garajim.Business.Seed;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class TenantWriteRulesTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private AuthManager CreateAuthManager()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Jwt:Key"] = "test-ortami-icin-en-az-32-karakterlik-gizli-anahtar",
                    ["Jwt:Issuer"] = "Garajim",
                    ["Jwt:Audience"] = "GarajimClient",
                    ["Jwt:ExpireDays"] = "7"
                })
                .Build();

            return new AuthManager(_db.UserDal, _db.CompanyDal, configuration, new SahteEpostaGonderici(), new BellekKodGonderimSayaci(), _db.UnitOfWork);
        }

        [Fact]
        public async Task Kayit_KullaniciyaKisiselSirketAcar()
        {
            var sonuc = await CreateAuthManager().RegisterAsync(new RegisterDto
            {
                Email = "yeni@garajim.local",
                FullName = "Ayşe Yılmaz",
                Password = "gizli123"
            });

            var kullanici = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == "yeni@garajim.local");
            var sirket = await _db.Context.Companies.AsNoTracking().SingleAsync(c => c.Id == kullanici.CompanyId);

            Assert.True(sonuc.Success);
            Assert.Equal("Ayşe Yılmaz", sirket.Name);
            Assert.Equal(PlanType.Bireysel, sirket.PlanType);
            Assert.True(kullanici.CompanyId > 0);
        }

        [Fact]
        public async Task Kayit_HerKullaniciyaAyriSirketAcar()
        {
            var manager = CreateAuthManager();

            await manager.RegisterAsync(new RegisterDto { Email = "bir@garajim.local", FullName = "Bir Kişi", Password = "gizli123" });
            await manager.RegisterAsync(new RegisterDto { Email = "iki@garajim.local", FullName = "İki Kişi", Password = "gizli123" });

            var birinci = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == "bir@garajim.local");
            var ikinci = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == "iki@garajim.local");

            Assert.NotEqual(birinci.CompanyId, ikinci.CompanyId);
        }

        [Fact]
        public async Task AracEklerken_KullanicininSirketiDevralinir()
        {
            var kullanici = _db.KullaniciEkle("surucu@garajim.local");
            var manager = new VehicleManager(_db.VehicleDal, _db.UserDal, _db.VehicleAccess, _db.CompanyDal, _db.PlanKurallari);

            var sonuc = await manager.AddAsync(kullanici.Id, new VehicleCreateDto
            {
                Plate = "34ABC123",
                Brand = "Renault",
                Model = "Clio",
                Year = 2018,
                CurrentKm = 100000,
                FuelType = FuelType.Benzin
            });

            var arac = await _db.Context.Vehicles.IgnoreQueryFilters().AsNoTracking().SingleAsync(v => v.Id == sonuc.Data.Id);

            Assert.True(sonuc.Success);
            Assert.Equal(kullanici.CompanyId, arac.CompanyId);
        }

        [Fact]
        public async Task BakimYakitMasrafHatirlatma_AracinSirketiniDevralir()
        {
            var kullanici = _db.KullaniciEkle("surucu@garajim.local");
            var arac = _db.AracEkle(kullanici.Id, "34ABC123");

            await new MaintenanceManager(_db.MaintenanceDal, _db.VehicleDal, _db.VehicleAccess, _db.PartDal, _db.UnitOfWork).AddAsync(kullanici.Id, new MaintenanceCreateDto
            {
                VehicleId = arac.Id,
                Type = MaintenanceType.PeriyodikBakim,
                Date = new DateTime(2026, 3, 1),
                Km = 110000,
                Cost = 4500m
            });
            await new FuelManager(_db.FuelDal, _db.VehicleDal, _db.VehicleAccess).AddAsync(kullanici.Id, new FuelCreateDto
            {
                VehicleId = arac.Id,
                Date = new DateTime(2026, 3, 2),
                Km = 111000,
                Liters = 40m,
                TotalCost = 1800m
            });
            await new ExpenseManager(_db.ExpenseDal, _db.VehicleAccess).AddAsync(kullanici.Id, new ExpenseCreateDto
            {
                VehicleId = arac.Id,
                Category = ExpenseCategory.Kasko,
                Date = new DateTime(2026, 3, 3),
                Amount = 12000m
            });
            await new ReminderManager(_db.ReminderDal, _db.VehicleAccess).AddAsync(kullanici.Id, new ReminderCreateDto
            {
                VehicleId = arac.Id,
                Type = ReminderType.Muayene,
                DueDate = new DateTime(2026, 9, 1)
            });

            var bakim = await _db.Context.MaintenanceRecords.IgnoreQueryFilters().AsNoTracking().SingleAsync();
            var yakit = await _db.Context.FuelRecords.IgnoreQueryFilters().AsNoTracking().SingleAsync();
            var masraf = await _db.Context.ExpenseRecords.IgnoreQueryFilters().AsNoTracking().SingleAsync();
            var hatirlatma = await _db.Context.Reminders.IgnoreQueryFilters().AsNoTracking().SingleAsync();

            Assert.Equal(arac.CompanyId, bakim.CompanyId);
            Assert.Equal(arac.CompanyId, yakit.CompanyId);
            Assert.Equal(arac.CompanyId, masraf.CompanyId);
            Assert.Equal(arac.CompanyId, hatirlatma.CompanyId);
        }

        [Fact]
        public async Task DemoSeed_GarajimDemoSirketiniAcar()
        {
            var seeder = _db.DemoSeeder();

            var eklendi = await seeder.RunAsync();

            var kullanici = await _db.Context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Email == DemoDataSeeder.DemoEmail);
            var sirket = await _db.Context.Companies.AsNoTracking().SingleAsync(c => c.Id == kullanici.CompanyId);
            var arac = await _db.Context.Vehicles.IgnoreQueryFilters().AsNoTracking().SingleAsync(v => v.CompanyId == sirket.Id);

            Assert.True(eklendi);
            Assert.Equal(DemoDataSeeder.DemoCompanyName, sirket.Name);
            Assert.Equal(sirket.Id, arac.CompanyId);
        }

        [Fact]
        public async Task DemoSeed_IkinciKosudaYeniSirketAcmaz()
        {
            var seeder = _db.DemoSeeder();

            await seeder.RunAsync();
            var sirketSayisi = await _db.Context.Companies.CountAsync();
            var ikinci = await seeder.RunAsync();

            Assert.False(ikinci);
            Assert.Equal(sirketSayisi, await _db.Context.Companies.CountAsync());
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
