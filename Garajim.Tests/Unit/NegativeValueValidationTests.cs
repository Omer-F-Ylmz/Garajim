using System.Linq.Expressions;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Moq;

namespace Garajim.Tests.Unit
{
    public class NegativeValueValidationTests
    {
        private const int UserId = 7;
        private const int VehicleId = 3;

        private readonly Mock<IVehicleDal> _vehicleDal = new Mock<IVehicleDal>();
        private readonly Mock<IVehicleAccessService> _vehicleAccess = new Mock<IVehicleAccessService>();
        private readonly Mock<IMaintenanceDal> _maintenanceDal = new Mock<IMaintenanceDal>();
        private readonly Mock<IFuelDal> _fuelDal = new Mock<IFuelDal>();
        private readonly Mock<IExpenseDal> _expenseDal = new Mock<IExpenseDal>();

        public NegativeValueValidationTests()
        {
            _vehicleAccess.Setup(d => d.GetAccessibleAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new Vehicle { Id = VehicleId, UserId = UserId, Plate = "34ABC123", CurrentKm = 100000 });
        }

        private MaintenanceCreateDto BakimDto()
        {
            return new MaintenanceCreateDto
            {
                VehicleId = VehicleId,
                Type = MaintenanceType.PeriyodikBakim,
                Date = new DateTime(2026, 3, 1),
                Km = 110000,
                Cost = 4500m
            };
        }

        private FuelCreateDto YakitDto()
        {
            return new FuelCreateDto
            {
                VehicleId = VehicleId,
                Date = new DateTime(2026, 3, 1),
                Km = 110000,
                Liters = 40m,
                TotalCost = 1800m
            };
        }

        private ExpenseCreateDto MasrafDto()
        {
            return new ExpenseCreateDto
            {
                VehicleId = VehicleId,
                Category = ExpenseCategory.Kasko,
                Date = new DateTime(2026, 3, 1),
                Amount = 12000m
            };
        }

        [Theory]
        [InlineData(-1, 110000)]
        [InlineData(-0.01, 110000)]
        [InlineData(4500, -1)]
        public async Task MaintenanceAdd_NegatifTutarVeyaKilometreReddedilir(decimal tutar, int km)
        {
            var manager = new MaintenanceManager(_maintenanceDal.Object, _vehicleDal.Object, _vehicleAccess.Object, new Mock<IMaintenancePartDal>().Object, new Mock<IDocumentDal>().Object, new Mock<IDocumentService>().Object, new Mock<IUnitOfWork>().Object);
            var dto = BakimDto();
            dto.Cost = tutar;
            dto.Km = km;

            var result = await manager.AddAsync(UserId, dto);

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            _maintenanceDal.Verify(d => d.AddAsync(It.IsAny<MaintenanceRecord>()), Times.Never);
        }

        [Fact]
        public async Task MaintenanceAdd_SifirTutarKabulEdilir()
        {
            _maintenanceDal.Setup(d => d.AddAsync(It.IsAny<MaintenanceRecord>())).Returns(Task.CompletedTask);
            var manager = new MaintenanceManager(_maintenanceDal.Object, _vehicleDal.Object, _vehicleAccess.Object, new Mock<IMaintenancePartDal>().Object, new Mock<IDocumentDal>().Object, new Mock<IDocumentService>().Object, new Mock<IUnitOfWork>().Object);
            var dto = BakimDto();
            dto.Cost = 0m;

            var result = await manager.AddAsync(UserId, dto);

            Assert.True(result.Success);
        }

        [Theory]
        [InlineData(0, 1800, 110000)]
        [InlineData(-5, 1800, 110000)]
        [InlineData(40, -1, 110000)]
        [InlineData(40, 1800, -1)]
        public async Task FuelAdd_GecersizLitreTutarVeyaKilometreReddedilir(decimal litre, decimal tutar, int km)
        {
            var manager = new FuelManager(_fuelDal.Object, _vehicleDal.Object, _vehicleAccess.Object);
            var dto = YakitDto();
            dto.Liters = litre;
            dto.TotalCost = tutar;
            dto.Km = km;

            var result = await manager.AddAsync(UserId, dto);

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            _fuelDal.Verify(d => d.AddAsync(It.IsAny<FuelRecord>()), Times.Never);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-12000)]
        public async Task ExpenseAdd_NegatifTutarReddedilir(decimal tutar)
        {
            var manager = new ExpenseManager(_expenseDal.Object, _vehicleAccess.Object);
            var dto = MasrafDto();
            dto.Amount = tutar;

            var result = await manager.AddAsync(UserId, dto);

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            _expenseDal.Verify(d => d.AddAsync(It.IsAny<ExpenseRecord>()), Times.Never);
        }

        [Fact]
        public async Task ExpenseAdd_SifirTutarKabulEdilir()
        {
            _expenseDal.Setup(d => d.AddAsync(It.IsAny<ExpenseRecord>())).Returns(Task.CompletedTask);
            var manager = new ExpenseManager(_expenseDal.Object, _vehicleAccess.Object);
            var dto = MasrafDto();
            dto.Amount = 0m;

            var result = await manager.AddAsync(UserId, dto);

            Assert.True(result.Success);
        }
    }
}
