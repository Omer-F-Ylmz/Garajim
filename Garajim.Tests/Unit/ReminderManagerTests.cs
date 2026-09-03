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
    public class ReminderManagerTests
    {
        private const int UserId = 7;
        private const int VehicleId = 3;

        private readonly Mock<IReminderDal> _reminderDal = new Mock<IReminderDal>();
        private readonly Mock<IVehicleDal> _vehicleDal = new Mock<IVehicleDal>();
        private readonly Mock<IVehicleAccessService> _vehicleAccess = new Mock<IVehicleAccessService>();

        private ReminderManager CreateManager()
        {
            return new ReminderManager(_reminderDal.Object, _vehicleAccess.Object);
        }

        private void AracSahibiOlarakAyarla()
        {
            _vehicleAccess.Setup(d => d.GetAccessibleAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new Vehicle { Id = VehicleId, UserId = UserId, Plate = "34ABC123", CurrentKm = 100000 });
        }

        [Fact]
        public async Task AddAsync_TarihVeKilometreBirlikteBossaReddedilir()
        {
            AracSahibiOlarakAyarla();

            var result = await CreateManager().AddAsync(UserId, new ReminderCreateDto
            {
                VehicleId = VehicleId,
                Type = ReminderType.Muayene,
                DueDate = null,
                DueKm = null,
                Note = "Muayene notu"
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.ReminderDateOrKmRequired, result.Message);
            _reminderDal.Verify(d => d.AddAsync(It.IsAny<Reminder>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_SadeceTarihVerilmesiYeterlidir()
        {
            AracSahibiOlarakAyarla();
            Reminder eklenen = null;
            _reminderDal.Setup(d => d.AddAsync(It.IsAny<Reminder>()))
                .Callback<Reminder>(reminder => eklenen = reminder)
                .Returns(Task.CompletedTask);

            var dueDate = DateTime.UtcNow.Date.AddDays(30);
            var result = await CreateManager().AddAsync(UserId, new ReminderCreateDto
            {
                VehicleId = VehicleId,
                Type = ReminderType.Muayene,
                DueDate = dueDate
            });

            Assert.True(result.Success);
            Assert.Equal(Messages.ReminderAdded, result.Message);
            Assert.Equal(dueDate, eklenen.DueDate);
            Assert.Null(eklenen.DueKm);
            Assert.False(eklenen.IsCompleted);
        }

        [Fact]
        public async Task AddAsync_SadeceKilometreVerilmesiYeterlidir()
        {
            AracSahibiOlarakAyarla();
            Reminder eklenen = null;
            _reminderDal.Setup(d => d.AddAsync(It.IsAny<Reminder>()))
                .Callback<Reminder>(reminder => eklenen = reminder)
                .Returns(Task.CompletedTask);

            var result = await CreateManager().AddAsync(UserId, new ReminderCreateDto
            {
                VehicleId = VehicleId,
                Type = ReminderType.PeriyodikBakim,
                DueKm = 150000
            });

            Assert.True(result.Success);
            Assert.Equal(150000, eklenen.DueKm);
            Assert.Null(eklenen.DueDate);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-150000)]
        public async Task AddAsync_SifirVeyaNegatifKilometreReddedilir(int dueKm)
        {
            AracSahibiOlarakAyarla();

            var result = await CreateManager().AddAsync(UserId, new ReminderCreateDto
            {
                VehicleId = VehicleId,
                Type = ReminderType.PeriyodikBakim,
                DueKm = dueKm
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            _reminderDal.Verify(d => d.AddAsync(It.IsAny<Reminder>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_TarihVarkenNegatifKilometreYineDeReddedilir()
        {
            AracSahibiOlarakAyarla();

            var result = await CreateManager().AddAsync(UserId, new ReminderCreateDto
            {
                VehicleId = VehicleId,
                Type = ReminderType.Muayene,
                DueDate = DateTime.UtcNow.Date.AddDays(30),
                DueKm = -5
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            _reminderDal.Verify(d => d.AddAsync(It.IsAny<Reminder>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_BaskaKullanicininAracinaHatirlatmaEklenemez()
        {
            _vehicleAccess.Setup(d => d.GetAccessibleAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((Vehicle)null);

            var result = await CreateManager().AddAsync(UserId, new ReminderCreateDto
            {
                VehicleId = VehicleId,
                Type = ReminderType.Kasko,
                DueDate = DateTime.UtcNow.Date.AddDays(10)
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.VehicleNotFound, result.Message);
            _reminderDal.Verify(d => d.AddAsync(It.IsAny<Reminder>()), Times.Never);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        [InlineData(400, 365)]
        [InlineData(30, 30)]
        public async Task GetUpcomingAsync_GunAraligiSinirlanir(int istenen, int beklenenGun)
        {
            DateTime kullanilanLimit = default;
            _reminderDal.Setup(d => d.GetUpcomingForUserAsync(UserId, It.IsAny<DateTime>()))
                .Callback<int, DateTime>((_, limit) => kullanilanLimit = limit)
                .ReturnsAsync(new List<UpcomingReminderDto>());

            var result = await CreateManager().GetUpcomingAsync(UserId, istenen);

            Assert.True(result.Success);
            Assert.Equal(Saat.BugunTr().AddDays(beklenenGun), kullanilanLimit);
        }
    }
}
