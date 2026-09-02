using Garajim.Business.Concrete;
using Garajim.Business.Seed;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class TenantInvariantTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private async Task<int> UyumsuzKayitSayisiAsync()
        {
            var araclar = await _db.Context.Vehicles.IgnoreQueryFilters().AsNoTracking()
                .ToDictionaryAsync(v => v.Id, v => v.CompanyId);

            var bakim = await _db.Context.MaintenanceRecords.IgnoreQueryFilters().AsNoTracking().ToListAsync();
            var yakit = await _db.Context.FuelRecords.IgnoreQueryFilters().AsNoTracking().ToListAsync();
            var masraf = await _db.Context.ExpenseRecords.IgnoreQueryFilters().AsNoTracking().ToListAsync();
            var hatirlatma = await _db.Context.Reminders.IgnoreQueryFilters().AsNoTracking().ToListAsync();

            return bakim.Count(k => araclar[k.VehicleId] != k.CompanyId)
                 + yakit.Count(k => araclar[k.VehicleId] != k.CompanyId)
                 + masraf.Count(k => araclar[k.VehicleId] != k.CompanyId)
                 + hatirlatma.Count(k => araclar[k.VehicleId] != k.CompanyId);
        }

        [Fact]
        public async Task ServisUzerindenYazilanKayitlarAracinSirketiyleUyumlu()
        {
            var sirketA = _db.SirketEkle("A Filo");
            var sirketB = _db.SirketEkle("B Filo");
            var kullaniciA = _db.KullaniciEkle("a@garajim.local", sirketA.Id);
            var kullaniciB = _db.KullaniciEkle("b@garajim.local", sirketB.Id);
            var aracA = _db.AracEkleSirketle(kullaniciA.Id, "34AAA111", sirketA.Id);
            var aracB = _db.AracEkleSirketle(kullaniciB.Id, "06BBB222", sirketB.Id);

            _db.Tenant.SetCompany(sirketA.Id);
            await new MaintenanceManager(_db.MaintenanceDal, _db.VehicleDal, _db.VehicleAccess, _db.PartDal, _db.UnitOfWork).AddAsync(kullaniciA.Id, new MaintenanceCreateDto
            {
                VehicleId = aracA.Id,
                Type = MaintenanceType.PeriyodikBakim,
                Date = new DateTime(2026, 3, 1),
                Km = 100000,
                Cost = 1000m
            });

            _db.Tenant.SetCompany(sirketB.Id);
            await new FuelManager(_db.FuelDal, _db.VehicleDal, _db.VehicleAccess).AddAsync(kullaniciB.Id, new FuelCreateDto
            {
                VehicleId = aracB.Id,
                Date = new DateTime(2026, 3, 2),
                Km = 100100,
                Liters = 40m,
                TotalCost = 1800m
            });

            Assert.Equal(0, await UyumsuzKayitSayisiAsync());
        }

        [Fact]
        public async Task DemoVerisiDegismeziBozmaz()
        {
            var seeder = _db.DemoSeeder();

            await seeder.RunAsync();

            Assert.Equal(0, await UyumsuzKayitSayisiAsync());
        }

        [Fact]
        public async Task ElleBozulanKayitDegismezKontrolundeYakalanir()
        {
            var sirketA = _db.SirketEkle("A Filo");
            var sirketB = _db.SirketEkle("B Filo");
            var kullaniciA = _db.KullaniciEkle("a@garajim.local", sirketA.Id);
            var aracA = _db.AracEkleSirketle(kullaniciA.Id, "34AAA111", sirketA.Id);

            _db.Context.MaintenanceRecords.Add(new Garajim.Entity.Concrete.MaintenanceRecord
            {
                CompanyId = sirketB.Id,
                VehicleId = aracA.Id,
                Type = MaintenanceType.PeriyodikBakim,
                Date = new DateTime(2026, 3, 1),
                Km = 100000,
                Cost = 1000m
            });
            await _db.Context.SaveChangesAsync();

            Assert.Equal(1, await UyumsuzKayitSayisiAsync());
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
