using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Integration
{
    public class DeleteLeakTests : IDisposable
    {
        private const int OlmayanId = 99999;

        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private readonly int _kullaniciA;
        private readonly int _kullaniciB;
        private readonly Vehicle _aracA;
        private readonly MaintenanceRecord _bakimA;
        private readonly FuelRecord _yakitA;
        private readonly ExpenseRecord _masrafA;
        private readonly Reminder _hatirlatmaA;

        public DeleteLeakTests()
        {
            _kullaniciA = _db.KullaniciEkle("a@garajim.local").Id;
            _kullaniciB = _db.KullaniciEkle("b@garajim.local", CompanyRole.Driver).Id;
            _aracA = _db.AracEkle(_kullaniciA, "34AAA111");

            _bakimA = new MaintenanceRecord { CompanyId = _aracA.CompanyId, VehicleId = _aracA.Id, Type = MaintenanceType.PeriyodikBakim, Date = new DateTime(2026, 3, 1), Km = 105000, Cost = 5000m };
            _yakitA = new FuelRecord { CompanyId = _aracA.CompanyId, VehicleId = _aracA.Id, Date = new DateTime(2026, 3, 2), Km = 106000, Liters = 40m, TotalCost = 1800m };
            _masrafA = new ExpenseRecord { CompanyId = _aracA.CompanyId, VehicleId = _aracA.Id, Category = ExpenseCategory.Kasko, Date = new DateTime(2026, 3, 3), Amount = 12000m };
            _hatirlatmaA = new Reminder { CompanyId = _aracA.CompanyId, VehicleId = _aracA.Id, Type = ReminderType.Muayene, DueDate = new DateTime(2026, 9, 1), CreatedAt = new DateTime(2026, 1, 1) };

            _db.Context.MaintenanceRecords.Add(_bakimA);
            _db.Context.FuelRecords.Add(_yakitA);
            _db.Context.ExpenseRecords.Add(_masrafA);
            _db.Context.Reminders.Add(_hatirlatmaA);
            _db.Context.SaveChanges();
        }

        [Fact]
        public async Task MaintenanceDelete_YabanciKayitVeOlmayanKayitAyniMesajiDoner()
        {
            var manager = new MaintenanceManager(_db.MaintenanceDal, _db.VehicleDal, _db.VehicleAccess, _db.PartDal, _db.UnitOfWork);

            var yabanci = await manager.DeleteAsync(_kullaniciB, _bakimA.Id);
            var olmayan = await manager.DeleteAsync(_kullaniciB, OlmayanId);

            Assert.False(yabanci.Success);
            Assert.False(olmayan.Success);
            Assert.Equal(olmayan.Message, yabanci.Message);
            Assert.Equal(Messages.RecordNotFound, yabanci.Message);
        }

        [Fact]
        public async Task FuelDelete_YabanciKayitVeOlmayanKayitAyniMesajiDoner()
        {
            var manager = new FuelManager(_db.FuelDal, _db.VehicleDal, _db.VehicleAccess);

            var yabanci = await manager.DeleteAsync(_kullaniciB, _yakitA.Id);
            var olmayan = await manager.DeleteAsync(_kullaniciB, OlmayanId);

            Assert.Equal(olmayan.Message, yabanci.Message);
            Assert.Equal(Messages.RecordNotFound, yabanci.Message);
        }

        [Fact]
        public async Task ExpenseDelete_YabanciKayitVeOlmayanKayitAyniMesajiDoner()
        {
            var manager = new ExpenseManager(_db.ExpenseDal, _db.VehicleAccess);

            var yabanci = await manager.DeleteAsync(_kullaniciB, _masrafA.Id);
            var olmayan = await manager.DeleteAsync(_kullaniciB, OlmayanId);

            Assert.Equal(olmayan.Message, yabanci.Message);
            Assert.Equal(Messages.RecordNotFound, yabanci.Message);
        }

        [Fact]
        public async Task ReminderDeleteVeComplete_YabanciKayitVeOlmayanKayitAyniMesajiDoner()
        {
            var manager = new ReminderManager(_db.ReminderDal, _db.VehicleAccess);

            var yabanciSil = await manager.DeleteAsync(_kullaniciB, _hatirlatmaA.Id);
            var olmayanSil = await manager.DeleteAsync(_kullaniciB, OlmayanId);
            var yabanciTamamla = await manager.CompleteAsync(_kullaniciB, _hatirlatmaA.Id);
            var olmayanTamamla = await manager.CompleteAsync(_kullaniciB, OlmayanId);

            Assert.Equal(olmayanSil.Message, yabanciSil.Message);
            Assert.Equal(olmayanTamamla.Message, yabanciTamamla.Message);
            Assert.Equal(Messages.ReminderNotFound, yabanciSil.Message);
        }

        [Fact]
        public async Task VehicleDelete_YabanciAracVeOlmayanAracAyniMesajiDoner()
        {
            var manager = new VehicleManager(_db.VehicleDal, _db.UserDal, _db.VehicleAccess, _db.CompanyDal, _db.PlanKurallari, _db.KmLogDal);

            var yabanci = await manager.DeleteAsync(_kullaniciB, _aracA.Id);
            var olmayan = await manager.DeleteAsync(_kullaniciB, OlmayanId);

            Assert.Equal(olmayan.Message, yabanci.Message);
            Assert.Equal(Messages.VehicleNotFound, yabanci.Message);
        }

        [Fact]
        public async Task YabanciSilmeDenemeleriKayitlariSilmez()
        {
            var maintenance = new MaintenanceManager(_db.MaintenanceDal, _db.VehicleDal, _db.VehicleAccess, _db.PartDal, _db.UnitOfWork);
            var fuel = new FuelManager(_db.FuelDal, _db.VehicleDal, _db.VehicleAccess);
            var expense = new ExpenseManager(_db.ExpenseDal, _db.VehicleAccess);
            var reminder = new ReminderManager(_db.ReminderDal, _db.VehicleAccess);
            var vehicle = new VehicleManager(_db.VehicleDal, _db.UserDal, _db.VehicleAccess, _db.CompanyDal, _db.PlanKurallari, _db.KmLogDal);

            await maintenance.DeleteAsync(_kullaniciB, _bakimA.Id);
            await fuel.DeleteAsync(_kullaniciB, _yakitA.Id);
            await expense.DeleteAsync(_kullaniciB, _masrafA.Id);
            await reminder.DeleteAsync(_kullaniciB, _hatirlatmaA.Id);
            await vehicle.DeleteAsync(_kullaniciB, _aracA.Id);

            Assert.Equal(1, _db.Context.MaintenanceRecords.Count());
            Assert.Equal(1, _db.Context.FuelRecords.Count());
            Assert.Equal(1, _db.Context.ExpenseRecords.Count());
            Assert.Equal(1, _db.Context.Reminders.Count());
            Assert.Equal(1, _db.Context.Vehicles.Count());
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
