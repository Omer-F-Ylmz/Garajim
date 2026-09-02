using System.Linq.Expressions;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Moq;

namespace Garajim.Tests.Unit
{
    public class ReportManagerFuelStatsTests
    {
        private const int UserId = 7;
        private const int VehicleId = 3;

        private readonly Mock<IVehicleDal> _vehicleDal = new Mock<IVehicleDal>();
        private readonly Mock<IVehicleAccessService> _vehicleAccess = new Mock<IVehicleAccessService>();
        private readonly Mock<IFuelDal> _fuelDal = new Mock<IFuelDal>();
        private readonly Mock<IMaintenanceDal> _maintenanceDal = new Mock<IMaintenanceDal>();
        private readonly Mock<IExpenseDal> _expenseDal = new Mock<IExpenseDal>();
        private readonly Mock<IUserDal> _userDal = new Mock<IUserDal>();
        private readonly Mock<ICompanyDal> _companyDal = new Mock<ICompanyDal>();
        private readonly Mock<IEvrakDal> _evrakDal = new Mock<IEvrakDal>();
        private readonly Mock<IReminderDal> _reminderDal = new Mock<IReminderDal>();
        private readonly Mock<IVehicleAssignmentDal> _assignmentDal = new Mock<IVehicleAssignmentDal>();
        private readonly Mock<IReceiptDraftDal> _receiptDraftDal = new Mock<IReceiptDraftDal>();
        private readonly Mock<ILastikDal> _lastikDal = new Mock<ILastikDal>();

        private ReportManager CreateManager(params FuelRecord[] kayitlar)
        {
            _vehicleAccess.Setup(d => d.GetAccessibleAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new Vehicle { Id = VehicleId, UserId = UserId, Plate = "34ABC123" });

            _fuelDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<FuelRecord, bool>>>()))
                .ReturnsAsync((Expression<Func<FuelRecord, bool>> predicate) =>
                    kayitlar.Where(predicate.Compile()).ToList());

            return new ReportManager(_vehicleAccess.Object, _maintenanceDal.Object, _fuelDal.Object, _expenseDal.Object, _userDal.Object, _companyDal.Object, _evrakDal.Object, _reminderDal.Object, _assignmentDal.Object, _receiptDraftDal.Object, TestPlanKurallari.Olustur(), _lastikDal.Object, TestEvrakKurallari.Olustur());
        }

        private static FuelRecord Kayit(int km, decimal litre, decimal tutar)
        {
            return new FuelRecord
            {
                VehicleId = VehicleId,
                Date = new DateTime(2026, 1, 1),
                Km = km,
                Liters = litre,
                TotalCost = tutar
            };
        }

        [Fact]
        public async Task GetFuelStatsAsync_TekKayitVarsaYetersizVeriDoner()
        {
            var result = await CreateManager(Kayit(10000, 40, 1000)).GetFuelStatsAsync(UserId, VehicleId);

            Assert.False(result.Success);
            Assert.Equal(Messages.NotEnoughFuelData, result.Message);
        }

        [Fact]
        public async Task GetFuelStatsAsync_KilometresiSifirOlanKayitSayilmadigiIcinYetersizVeriDoner()
        {
            var result = await CreateManager(Kayit(0, 40, 1000), Kayit(10000, 45, 1200)).GetFuelStatsAsync(UserId, VehicleId);

            Assert.False(result.Success);
            Assert.Equal(Messages.NotEnoughFuelData, result.Message);
        }

        [Fact]
        public async Task GetFuelStatsAsync_KilometreFarkiSifirsaYetersizVeriDoner()
        {
            var result = await CreateManager(Kayit(10000, 40, 1000), Kayit(10000, 45, 1200)).GetFuelStatsAsync(UserId, VehicleId);

            Assert.False(result.Success);
            Assert.Equal(Messages.NotEnoughFuelData, result.Message);
        }

        [Fact]
        public async Task GetFuelStatsAsync_TuketimVeMaliyetKilometreFarkinaGoreHesaplanir()
        {
            var manager = CreateManager(
                Kayit(11000, 35, 1050),
                Kayit(10000, 40, 1000),
                Kayit(10500, 30, 900));

            var result = await manager.GetFuelStatsAsync(UserId, VehicleId);

            Assert.True(result.Success);
            Assert.Equal(1000, result.Data.TotalKm);
            Assert.Equal(105m, result.Data.TotalLiters, 2);
            Assert.Equal(2950m, result.Data.TotalCost, 2);
            Assert.Equal(6.5m, result.Data.AverageConsumptionPer100Km, 2);
            Assert.Equal(1.95m, result.Data.CostPerKm, 2);
        }

        [Fact]
        public async Task GetFuelStatsAsync_SifirKilometreliKayitHesabaKatilmaz()
        {
            var manager = CreateManager(
                Kayit(0, 99, 9999),
                Kayit(10000, 40, 1000),
                Kayit(10500, 30, 900),
                Kayit(11000, 35, 1050));

            var result = await manager.GetFuelStatsAsync(UserId, VehicleId);

            Assert.True(result.Success);
            Assert.Equal(1000, result.Data.TotalKm);
            Assert.Equal(105m, result.Data.TotalLiters, 2);
            Assert.Equal(6.5m, result.Data.AverageConsumptionPer100Km, 2);
        }

        [Fact]
        public async Task GetFuelStatsAsync_BaskaKullanicininAraciIcinHataDoner()
        {
            _vehicleAccess.Setup(d => d.GetAccessibleAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((Vehicle)null);
            var manager = new ReportManager(_vehicleAccess.Object, _maintenanceDal.Object, _fuelDal.Object, _expenseDal.Object, _userDal.Object, _companyDal.Object, _evrakDal.Object, _reminderDal.Object, _assignmentDal.Object, _receiptDraftDal.Object, TestPlanKurallari.Olustur(), _lastikDal.Object, TestEvrakKurallari.Olustur());

            var result = await manager.GetFuelStatsAsync(UserId, VehicleId);

            Assert.False(result.Success);
            Assert.Equal(Messages.VehicleNotFound, result.Message);
        }
    }
}
