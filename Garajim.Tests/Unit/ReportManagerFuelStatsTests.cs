using System.Linq.Expressions;
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
        private readonly Mock<IFuelDal> _fuelDal = new Mock<IFuelDal>();
        private readonly Mock<IMaintenanceDal> _maintenanceDal = new Mock<IMaintenanceDal>();
        private readonly Mock<IExpenseDal> _expenseDal = new Mock<IExpenseDal>();

        private ReportManager CreateManager(params FuelRecord[] kayitlar)
        {
            _vehicleDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(new Vehicle { Id = VehicleId, UserId = UserId, Plate = "34ABC123" });

            _fuelDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<FuelRecord, bool>>>()))
                .ReturnsAsync((Expression<Func<FuelRecord, bool>> predicate) =>
                    kayitlar.Where(predicate.Compile()).ToList());

            return new ReportManager(_vehicleDal.Object, _maintenanceDal.Object, _fuelDal.Object, _expenseDal.Object);
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
            _vehicleDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Vehicle, bool>>>())).ReturnsAsync((Vehicle)null);
            var manager = new ReportManager(_vehicleDal.Object, _maintenanceDal.Object, _fuelDal.Object, _expenseDal.Object);

            var result = await manager.GetFuelStatsAsync(UserId, VehicleId);

            Assert.False(result.Success);
            Assert.Equal(Messages.VehicleNotFound, result.Message);
        }
    }
}
