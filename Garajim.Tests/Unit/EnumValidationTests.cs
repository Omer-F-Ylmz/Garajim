using System.Linq.Expressions;
using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Moq;

namespace Garajim.Tests.Unit
{
    public class EnumValidationTests
    {
        private const int UserId = 7;
        private const int VehicleId = 3;

        private readonly Mock<IVehicleDal> _vehicleDal = new Mock<IVehicleDal>();
        private readonly Mock<IUserDal> _userDal = new Mock<IUserDal>();
        private readonly Mock<IMaintenanceDal> _maintenanceDal = new Mock<IMaintenanceDal>();
        private readonly Mock<IExpenseDal> _expenseDal = new Mock<IExpenseDal>();
        private readonly Mock<IReminderDal> _reminderDal = new Mock<IReminderDal>();

        public EnumValidationTests()
        {
            _vehicleDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(new Vehicle { Id = VehicleId, UserId = UserId, Plate = "34ABC123", Brand = "Renault", Model = "Clio", Year = 2018, CurrentKm = 100000 });
            _vehicleDal.Setup(d => d.AnyAsync(It.IsAny<Expression<Func<Vehicle, bool>>>())).ReturnsAsync(false);
            _userDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<AppUser, bool>>>())).ReturnsAsync(new AppUser { Id = UserId, CompanyId = 42 });
            _vehicleDal.Setup(d => d.AddAsync(It.IsAny<Vehicle>())).Returns(Task.CompletedTask);
            _vehicleDal.Setup(d => d.UpdateAsync(It.IsAny<Vehicle>())).Returns(Task.CompletedTask);
            _maintenanceDal.Setup(d => d.AddAsync(It.IsAny<MaintenanceRecord>())).Returns(Task.CompletedTask);
            _expenseDal.Setup(d => d.AddAsync(It.IsAny<ExpenseRecord>())).Returns(Task.CompletedTask);
            _reminderDal.Setup(d => d.AddAsync(It.IsAny<Reminder>())).Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task VehicleAdd_TanimsizYakitTipiReddedilir()
        {
            var manager = new VehicleManager(_vehicleDal.Object, _userDal.Object);

            var result = await manager.AddAsync(UserId, new VehicleCreateDto
            {
                Plate = "34ABC123",
                Brand = "Renault",
                Model = "Clio",
                Year = 2018,
                CurrentKm = 120000,
                FuelType = (FuelType)999
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            _vehicleDal.Verify(d => d.AddAsync(It.IsAny<Vehicle>()), Times.Never);
        }

        [Fact]
        public async Task VehicleUpdate_TanimsizYakitTipiReddedilir()
        {
            var manager = new VehicleManager(_vehicleDal.Object, _userDal.Object);

            var result = await manager.UpdateAsync(UserId, VehicleId, new VehicleUpdateDto
            {
                Brand = "Renault",
                Model = "Clio",
                Year = 2018,
                CurrentKm = 120000,
                FuelType = (FuelType)0
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            _vehicleDal.Verify(d => d.UpdateAsync(It.IsAny<Vehicle>()), Times.Never);
        }

        [Fact]
        public async Task MaintenanceAdd_TanimsizBakimTuruReddedilir()
        {
            var manager = new MaintenanceManager(_maintenanceDal.Object, _vehicleDal.Object);

            var result = await manager.AddAsync(UserId, new MaintenanceCreateDto
            {
                VehicleId = VehicleId,
                Type = (MaintenanceType)999,
                Date = new DateTime(2026, 3, 1),
                Km = 110000,
                Cost = 4500m
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            _maintenanceDal.Verify(d => d.AddAsync(It.IsAny<MaintenanceRecord>()), Times.Never);
        }

        [Fact]
        public async Task ExpenseAdd_TanimsizKategoriReddedilir()
        {
            var manager = new ExpenseManager(_expenseDal.Object, _vehicleDal.Object);

            var result = await manager.AddAsync(UserId, new ExpenseCreateDto
            {
                VehicleId = VehicleId,
                Category = (ExpenseCategory)999,
                Date = new DateTime(2026, 3, 1),
                Amount = 12000m
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            _expenseDal.Verify(d => d.AddAsync(It.IsAny<ExpenseRecord>()), Times.Never);
        }

        [Fact]
        public async Task ReminderAdd_TanimsizHatirlatmaTuruReddedilir()
        {
            var manager = new ReminderManager(_reminderDal.Object, _vehicleDal.Object);

            var result = await manager.AddAsync(UserId, new ReminderCreateDto
            {
                VehicleId = VehicleId,
                Type = (ReminderType)999,
                DueDate = DateTime.UtcNow.Date.AddDays(10)
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            _reminderDal.Verify(d => d.AddAsync(It.IsAny<Reminder>()), Times.Never);
        }

        [Fact]
        public async Task TanimliEnumDegerleriKabulEdilmeyeDevamEder()
        {
            var vehicleManager = new VehicleManager(_vehicleDal.Object, _userDal.Object);
            var maintenanceManager = new MaintenanceManager(_maintenanceDal.Object, _vehicleDal.Object);
            var expenseManager = new ExpenseManager(_expenseDal.Object, _vehicleDal.Object);
            var reminderManager = new ReminderManager(_reminderDal.Object, _vehicleDal.Object);

            var vehicle = await vehicleManager.AddAsync(UserId, new VehicleCreateDto
            {
                Plate = "34ABC123",
                Brand = "Renault",
                Model = "Clio",
                Year = 2018,
                CurrentKm = 120000,
                FuelType = FuelType.Dizel
            });
            var maintenance = await maintenanceManager.AddAsync(UserId, new MaintenanceCreateDto
            {
                VehicleId = VehicleId,
                Type = MaintenanceType.YagDegisimi,
                Date = new DateTime(2026, 3, 1),
                Km = 110000,
                Cost = 4500m
            });
            var expense = await expenseManager.AddAsync(UserId, new ExpenseCreateDto
            {
                VehicleId = VehicleId,
                Category = ExpenseCategory.Mtv,
                Date = new DateTime(2026, 3, 1),
                Amount = 12000m
            });
            var reminder = await reminderManager.AddAsync(UserId, new ReminderCreateDto
            {
                VehicleId = VehicleId,
                Type = ReminderType.Kasko,
                DueDate = DateTime.UtcNow.Date.AddDays(10)
            });

            Assert.True(vehicle.Success);
            Assert.True(maintenance.Success);
            Assert.True(expense.Success);
            Assert.True(reminder.Success);
        }
    }
}
