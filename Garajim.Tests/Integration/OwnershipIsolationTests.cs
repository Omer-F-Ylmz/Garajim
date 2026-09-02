using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class OwnershipIsolationTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private readonly int _kullaniciA;
        private readonly int _kullaniciB;
        private readonly Vehicle _aracA;
        private readonly MaintenanceRecord _bakimA;
        private readonly FuelRecord _yakitA;
        private readonly ExpenseRecord _masrafA;
        private readonly Reminder _hatirlatmaA;

        public OwnershipIsolationTests()
        {
            _kullaniciA = _db.KullaniciEkle("a@garajim.local").Id;
            _kullaniciB = _db.KullaniciEkle("b@garajim.local", CompanyRole.Driver).Id;
            _aracA = _db.AracEkle(_kullaniciA, "34AAA111");
            _db.AracEkle(_kullaniciB, "06BBB222");

            _bakimA = new MaintenanceRecord
            {
                CompanyId = _aracA.CompanyId, VehicleId = _aracA.Id,
                Type = MaintenanceType.PeriyodikBakim,
                Date = new DateTime(2026, 3, 1),
                Km = 105000,
                Cost = 5000m,
                ServiceName = "Servis"
            };
            _yakitA = new FuelRecord
            {
                CompanyId = _aracA.CompanyId, VehicleId = _aracA.Id,
                Date = new DateTime(2026, 3, 2),
                Km = 106000,
                Liters = 40m,
                TotalCost = 1800m
            };
            _masrafA = new ExpenseRecord
            {
                CompanyId = _aracA.CompanyId, VehicleId = _aracA.Id,
                Category = ExpenseCategory.Kasko,
                Date = new DateTime(2026, 3, 3),
                Amount = 12000m
            };
            _hatirlatmaA = new Reminder
            {
                CompanyId = _aracA.CompanyId, VehicleId = _aracA.Id,
                Type = ReminderType.Muayene,
                DueDate = DateTime.UtcNow.Date.AddDays(10),
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 1, 1)
            };

            _db.Context.MaintenanceRecords.Add(_bakimA);
            _db.Context.FuelRecords.Add(_yakitA);
            _db.Context.ExpenseRecords.Add(_masrafA);
            _db.Context.Reminders.Add(_hatirlatmaA);
            _db.Context.SaveChanges();
        }

        [Fact]
        public async Task VehicleManager_BaskaKullanicininAracinaErisemez()
        {
            var manager = new VehicleManager(_db.VehicleDal, _db.UserDal, _db.VehicleAccess, _db.CompanyDal, _db.PlanKurallari);

            var getir = await manager.GetByIdAsync(_kullaniciB, _aracA.Id);
            var listele = await manager.GetAllAsync(_kullaniciB);
            var guncelle = await manager.UpdateAsync(_kullaniciB, _aracA.Id, new VehicleUpdateDto
            {
                Brand = "Ele Geçirildi",
                Model = "Ele Geçirildi",
                Year = 2020,
                CurrentKm = 999999
            });
            var sil = await manager.DeleteAsync(_kullaniciB, _aracA.Id);

            Assert.False(getir.Success);
            Assert.Equal(Messages.VehicleNotFound, getir.Message);
            Assert.False(guncelle.Success);
            Assert.False(sil.Success);
            Assert.DoesNotContain(listele.Data, v => v.Id == _aracA.Id);
            Assert.Empty(listele.Data);

            var veritabanindaki = _db.AraciYenidenOku(_aracA.Id);
            Assert.Equal("Renault", veritabanindaki.Brand);
            Assert.Equal(100000, veritabanindaki.CurrentKm);
        }

        [Fact]
        public async Task MaintenanceManager_BaskaKullanicininBakimKayitlarinaErisemez()
        {
            var manager = new MaintenanceManager(_db.MaintenanceDal, _db.VehicleDal, _db.VehicleAccess, _db.PartDal, _db.UnitOfWork);

            var listele = await manager.GetListAsync(_kullaniciB, _aracA.Id);
            var ekle = await manager.AddAsync(_kullaniciB, new MaintenanceCreateDto
            {
                VehicleId = _aracA.Id,
                Type = MaintenanceType.YagDegisimi,
                Date = new DateTime(2026, 4, 1),
                Km = 110000,
                Cost = 1000m
            });
            var sil = await manager.DeleteAsync(_kullaniciB, _bakimA.Id);

            Assert.False(listele.Success);
            Assert.Equal(Messages.VehicleNotFound, listele.Message);
            Assert.False(ekle.Success);
            Assert.False(sil.Success);
            Assert.Equal(Messages.RecordNotFound, sil.Message);
            Assert.Equal(1, await _db.Context.MaintenanceRecords.CountAsync(m => m.VehicleId == _aracA.Id));
        }

        [Fact]
        public async Task FuelManager_BaskaKullanicininYakitKayitlarinaErisemez()
        {
            var manager = new FuelManager(_db.FuelDal, _db.VehicleDal, _db.VehicleAccess);

            var listele = await manager.GetListAsync(_kullaniciB, _aracA.Id);
            var ekle = await manager.AddAsync(_kullaniciB, new FuelCreateDto
            {
                VehicleId = _aracA.Id,
                Date = new DateTime(2026, 4, 1),
                Km = 111000,
                Liters = 30m,
                TotalCost = 1400m
            });
            var sil = await manager.DeleteAsync(_kullaniciB, _yakitA.Id);

            Assert.False(listele.Success);
            Assert.False(ekle.Success);
            Assert.False(sil.Success);
            Assert.Equal(1, await _db.Context.FuelRecords.CountAsync(f => f.VehicleId == _aracA.Id));
        }

        [Fact]
        public async Task ExpenseManager_BaskaKullanicininMasrafKayitlarinaErisemez()
        {
            var manager = new ExpenseManager(_db.ExpenseDal, _db.VehicleAccess);

            var listele = await manager.GetListAsync(_kullaniciB, _aracA.Id);
            var ekle = await manager.AddAsync(_kullaniciB, new ExpenseCreateDto
            {
                VehicleId = _aracA.Id,
                Category = ExpenseCategory.Otopark,
                Date = new DateTime(2026, 4, 1),
                Amount = 500m
            });
            var sil = await manager.DeleteAsync(_kullaniciB, _masrafA.Id);

            Assert.False(listele.Success);
            Assert.False(ekle.Success);
            Assert.False(sil.Success);
            Assert.Equal(1, await _db.Context.ExpenseRecords.CountAsync(e => e.VehicleId == _aracA.Id));
        }

        [Fact]
        public async Task ReminderManager_BaskaKullanicininHatirlatmalarinaErisemez()
        {
            var manager = new ReminderManager(_db.ReminderDal, _db.VehicleAccess);

            var listele = await manager.GetListAsync(_kullaniciB, _aracA.Id);
            var yaklasan = await manager.GetUpcomingAsync(_kullaniciB, 30);
            var tamamla = await manager.CompleteAsync(_kullaniciB, _hatirlatmaA.Id);
            var sil = await manager.DeleteAsync(_kullaniciB, _hatirlatmaA.Id);

            Assert.False(listele.Success);
            Assert.True(yaklasan.Success);
            Assert.Empty(yaklasan.Data);
            Assert.False(tamamla.Success);
            Assert.Equal(Messages.ReminderNotFound, tamamla.Message);
            Assert.False(sil.Success);

            var veritabanindaki = await _db.Context.Reminders.AsNoTracking().SingleAsync(r => r.Id == _hatirlatmaA.Id);
            Assert.False(veritabanindaki.IsCompleted);
        }

        [Fact]
        public async Task ReportManager_BaskaKullanicininRaporlarinaErisemez()
        {
            var manager = new ReportManager(_db.VehicleAccess, _db.MaintenanceDal, _db.FuelDal, _db.ExpenseDal, _db.UserDal, _db.CompanyDal, _db.EvrakDal, _db.ReminderDal, _db.AssignmentDal, _db.ReceiptDraftDal, _db.PlanKurallari, _db.LastikDal, _db.EvrakKurallari, _db.HasarDosyasiDal, _db.DegerService);

            var ozet = await manager.GetSummaryAsync(_kullaniciB, _aracA.Id, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
            var aylik = await manager.GetMonthlyAsync(_kullaniciB, _aracA.Id);
            var yakit = await manager.GetFuelStatsAsync(_kullaniciB, _aracA.Id);

            Assert.False(ozet.Success);
            Assert.Equal(Messages.VehicleNotFound, ozet.Message);
            Assert.False(aylik.Success);
            Assert.False(yakit.Success);
        }

        [Fact]
        public async Task Sahibi_KendiKayitlarinaErisebilir()
        {
            var vehicleManager = new VehicleManager(_db.VehicleDal, _db.UserDal, _db.VehicleAccess, _db.CompanyDal, _db.PlanKurallari);
            var maintenanceManager = new MaintenanceManager(_db.MaintenanceDal, _db.VehicleDal, _db.VehicleAccess, _db.PartDal, _db.UnitOfWork);
            var reminderManager = new ReminderManager(_db.ReminderDal, _db.VehicleAccess);

            var arac = await vehicleManager.GetByIdAsync(_kullaniciA, _aracA.Id);
            var bakimlar = await maintenanceManager.GetListAsync(_kullaniciA, _aracA.Id);
            var yaklasan = await reminderManager.GetUpcomingAsync(_kullaniciA, 30);

            Assert.True(arac.Success);
            Assert.Equal("34AAA111", arac.Data.Plate);
            Assert.True(bakimlar.Success);
            Assert.Single(bakimlar.Data);
            Assert.Single(yaklasan.Data);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
