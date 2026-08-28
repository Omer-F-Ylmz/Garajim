using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Integration
{
    public class ListOrderingAndLimitTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private readonly int _userId;
        private readonly Vehicle _arac;

        public ListOrderingAndLimitTests()
        {
            _userId = _db.KullaniciEkle("a@garajim.local").Id;
            _arac = _db.AracEkle(_userId, "34AAA111");
        }

        private void BakimEkle(int adet)
        {
            for (var i = 0; i < adet; i++)
            {
                _db.Context.MaintenanceRecords.Add(new MaintenanceRecord
                {
                    CompanyId = _arac.CompanyId, VehicleId = _arac.Id,
                    Type = MaintenanceType.PeriyodikBakim,
                    Date = new DateTime(2020, 1, 1).AddDays(i),
                    Km = 100000 + i,
                    Cost = 1000m
                });
            }

            _db.Context.SaveChanges();
        }

        [Fact]
        public async Task BakimListesiEnYeniKayittanBaslar()
        {
            BakimEkle(10);
            var manager = new MaintenanceManager(_db.MaintenanceDal, _db.VehicleDal, _db.VehicleAccess);

            var result = await manager.GetListAsync(_userId, _arac.Id);

            Assert.True(result.Success);
            Assert.Equal(10, result.Data.Count);
            Assert.Equal(new DateTime(2020, 1, 10), result.Data[0].Date);
            Assert.Equal(new DateTime(2020, 1, 1), result.Data[result.Data.Count - 1].Date);
        }

        [Fact]
        public async Task BakimListesiUstSinirdaKesilir()
        {
            BakimEkle(QueryLimits.MaxListSize + 25);
            var manager = new MaintenanceManager(_db.MaintenanceDal, _db.VehicleDal, _db.VehicleAccess);

            var result = await manager.GetListAsync(_userId, _arac.Id);

            Assert.Equal(QueryLimits.MaxListSize, result.Data.Count);
            Assert.Equal(new DateTime(2020, 1, 1).AddDays(QueryLimits.MaxListSize + 24), result.Data[0].Date);
        }

        [Fact]
        public async Task YakitVeMasrafListeleriEnYeniKayittanBaslar()
        {
            _db.Context.FuelRecords.AddRange(
                new FuelRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Date = new DateTime(2026, 1, 5), Km = 101000, Liters = 40m, TotalCost = 1800m },
                new FuelRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Date = new DateTime(2026, 3, 5), Km = 102000, Liters = 30m, TotalCost = 1400m });
            _db.Context.ExpenseRecords.AddRange(
                new ExpenseRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Category = ExpenseCategory.Otopark, Date = new DateTime(2026, 1, 8), Amount = 200m },
                new ExpenseRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Category = ExpenseCategory.Kasko, Date = new DateTime(2026, 4, 8), Amount = 12000m });
            _db.Context.SaveChanges();

            var fuel = await new FuelManager(_db.FuelDal, _db.VehicleDal, _db.VehicleAccess).GetListAsync(_userId, _arac.Id);
            var expense = await new ExpenseManager(_db.ExpenseDal, _db.VehicleAccess).GetListAsync(_userId, _arac.Id);

            Assert.Equal(new DateTime(2026, 3, 5), fuel.Data[0].Date);
            Assert.Equal(new DateTime(2026, 4, 8), expense.Data[0].Date);
        }

        [Fact]
        public async Task HatirlatmaListesiOnceBekleyenSonraTarihSirasinda()
        {
            _db.Context.Reminders.AddRange(
                new Reminder { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Type = ReminderType.Kasko, DueDate = new DateTime(2026, 2, 1), IsCompleted = true, CreatedAt = new DateTime(2026, 1, 1) },
                new Reminder { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Type = ReminderType.Muayene, DueDate = new DateTime(2026, 9, 1), IsCompleted = false, CreatedAt = new DateTime(2026, 1, 1) },
                new Reminder { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Type = ReminderType.Mtv, DueDate = new DateTime(2026, 5, 1), IsCompleted = false, CreatedAt = new DateTime(2026, 1, 1) });
            _db.Context.SaveChanges();

            var result = await new ReminderManager(_db.ReminderDal, _db.VehicleAccess).GetListAsync(_userId, _arac.Id);

            Assert.Equal(3, result.Data.Count);
            Assert.False(result.Data[0].IsCompleted);
            Assert.Equal(new DateTime(2026, 5, 1), result.Data[0].DueDate);
            Assert.Equal(new DateTime(2026, 9, 1), result.Data[1].DueDate);
            Assert.True(result.Data[2].IsCompleted);
        }

        [Fact]
        public async Task ListeSorgulari_DegisiklikIzlemeyeKayitBirakmaz()
        {
            BakimEkle(5);
            _db.Context.ChangeTracker.Clear();

            await new MaintenanceManager(_db.MaintenanceDal, _db.VehicleDal, _db.VehicleAccess).GetListAsync(_userId, _arac.Id);

            Assert.Empty(_db.Context.ChangeTracker.Entries<MaintenanceRecord>());
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
