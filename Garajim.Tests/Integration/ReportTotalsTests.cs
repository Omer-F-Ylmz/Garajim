using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Integration
{
    public class ReportTotalsTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private readonly int _userId;
        private readonly Vehicle _arac;

        public ReportTotalsTests()
        {
            _userId = _db.KullaniciEkle("a@garajim.local").Id;
            _arac = _db.AracEkle(_userId, "34AAA111");

            _db.Context.FuelRecords.AddRange(
                new FuelRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Date = new DateTime(2026, 3, 1), Km = 100500, Liters = 40m, TotalCost = 1800m },
                new FuelRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Date = new DateTime(2026, 3, 15), Km = 101000, Liters = 30m, TotalCost = 1350m },
                new FuelRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Date = new DateTime(2026, 4, 10), Km = 101800, Liters = 45m, TotalCost = 2050m });

            _db.Context.MaintenanceRecords.AddRange(
                new MaintenanceRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Type = MaintenanceType.PeriyodikBakim, Date = new DateTime(2026, 3, 20), Km = 101200, Cost = 5000m },
                new MaintenanceRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Type = MaintenanceType.LastikDegisimi, Date = new DateTime(2026, 4, 5), Km = 101600, Cost = 8000m });

            _db.Context.ExpenseRecords.AddRange(
                new ExpenseRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Category = ExpenseCategory.Kasko, Date = new DateTime(2026, 3, 2), Amount = 12000m },
                new ExpenseRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Category = ExpenseCategory.Otopark, Date = new DateTime(2026, 3, 8), Amount = 300m },
                new ExpenseRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Category = ExpenseCategory.Otopark, Date = new DateTime(2026, 3, 31, 22, 30, 0), Amount = 200m },
                new ExpenseRecord { CompanyId = _arac.CompanyId, VehicleId = _arac.Id, Category = ExpenseCategory.Mtv, Date = new DateTime(2026, 5, 1), Amount = 4000m });

            _db.Context.SaveChanges();
        }

        private ReportManager CreateManager()
        {
            return new ReportManager(_db.VehicleAccess, _db.MaintenanceDal, _db.FuelDal, _db.ExpenseDal, _db.UserDal, _db.CompanyDal, _db.EvrakDal, _db.ReminderDal, _db.AssignmentDal, _db.ReceiptDraftDal, _db.PlanKurallari, _db.LastikDal, _db.EvrakKurallari, _db.HasarDosyasiDal, _db.DegerService);
        }

        [Fact]
        public async Task GetSummaryAsync_AraliktakiTumKalemleriToplar()
        {
            var result = await CreateManager().GetSummaryAsync(_userId, _arac.Id, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

            Assert.True(result.Success);
            Assert.Equal(3150m, result.Data.TotalFuel, 2);
            Assert.Equal(5000m, result.Data.TotalMaintenance, 2);
            Assert.Equal(12500m, result.Data.TotalOtherExpense, 2);
            Assert.Equal(20650m, result.Data.GrandTotal, 2);
        }

        [Fact]
        public async Task GetSummaryAsync_BitisGunuGunSonunaKadarDahildir()
        {
            var result = await CreateManager().GetSummaryAsync(_userId, _arac.Id, new DateTime(2026, 3, 31), new DateTime(2026, 3, 31));

            Assert.True(result.Success);
            Assert.Equal(200m, result.Data.TotalOtherExpense, 2);
        }

        [Fact]
        public async Task GetSummaryAsync_KategoriToplamlariBuyuktenKucugeSiralanir()
        {
            var result = await CreateManager().GetSummaryAsync(_userId, _arac.Id, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

            Assert.True(result.Success);
            Assert.Equal(3, result.Data.Categories.Count);
            Assert.Equal(ExpenseCategory.Kasko.ToString(), result.Data.Categories[0].Category);
            Assert.Equal(12000m, result.Data.Categories[0].Total, 2);
            Assert.Equal(ExpenseCategory.Mtv.ToString(), result.Data.Categories[1].Category);
            Assert.Equal(500m, result.Data.Categories[2].Total, 2);
            Assert.Equal(16500m, result.Data.TotalOtherExpense, 2);
        }

        [Fact]
        public async Task GetSummaryAsync_BitisBaslangictanKucukseHataDoner()
        {
            var result = await CreateManager().GetSummaryAsync(_userId, _arac.Id, new DateTime(2026, 5, 1), new DateTime(2026, 4, 1));

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
        }

        [Fact]
        public async Task GetMonthlyAsync_UcKaynagiAyBazindaBirlestirir()
        {
            var result = await CreateManager().GetMonthlyAsync(_userId, _arac.Id);

            Assert.True(result.Success);
            Assert.Equal(3, result.Data.Count);

            var mart = result.Data.Single(m => m.Year == 2026 && m.Month == 3);
            var nisan = result.Data.Single(m => m.Year == 2026 && m.Month == 4);
            var mayis = result.Data.Single(m => m.Year == 2026 && m.Month == 5);

            Assert.Equal(20650m, mart.Total, 2);
            Assert.Equal(10050m, nisan.Total, 2);
            Assert.Equal(4000m, mayis.Total, 2);
            Assert.Equal(3, result.Data.Select(m => m.Month).Distinct().Count());
        }

        [Fact]
        public async Task GetFuelStatsAsync_GercekVeriUzerindeTuketimHesaplar()
        {
            var result = await CreateManager().GetFuelStatsAsync(_userId, _arac.Id);

            Assert.True(result.Success);
            Assert.Equal(1300, result.Data.TotalKm);
            Assert.Equal(115m, result.Data.TotalLiters, 2);
            Assert.Equal(5200m, result.Data.TotalCost, 2);
            Assert.Equal(5.77m, result.Data.AverageConsumptionPer100Km, 2);
            Assert.Equal(2.62m, result.Data.CostPerKm, 2);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
