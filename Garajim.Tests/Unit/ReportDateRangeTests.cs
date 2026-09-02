using System.Linq.Expressions;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Moq;

namespace Garajim.Tests.Unit
{
    public class ReportDateRangeTests
    {
        private const int UserId = 7;
        private const int VehicleId = 3;

        private readonly Mock<IVehicleAccessService> _vehicleAccess = new Mock<IVehicleAccessService>();
        private readonly Mock<IMaintenanceDal> _maintenanceDal = new Mock<IMaintenanceDal>();
        private readonly Mock<IFuelDal> _fuelDal = new Mock<IFuelDal>();
        private readonly Mock<IExpenseDal> _expenseDal = new Mock<IExpenseDal>();
        private readonly Mock<IUserDal> _userDal = new Mock<IUserDal>();
        private readonly Mock<ICompanyDal> _companyDal = new Mock<ICompanyDal>();
        private readonly Mock<IEvrakDal> _evrakDal = new Mock<IEvrakDal>();
        private readonly Mock<IReminderDal> _reminderDal = new Mock<IReminderDal>();
        private readonly Mock<IVehicleAssignmentDal> _assignmentDal = new Mock<IVehicleAssignmentDal>();
        private readonly Mock<IReceiptDraftDal> _receiptDraftDal = new Mock<IReceiptDraftDal>();
        private readonly Mock<ILastikDal> _lastikDal = new Mock<ILastikDal>();

        private ReportManager CreateManager()
        {
            _vehicleAccess.Setup(d => d.GetAccessibleAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new Vehicle { Id = VehicleId, UserId = UserId, Plate = "34ABC123" });
            _fuelDal.Setup(d => d.GetTotalCostAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(100m);
            _maintenanceDal.Setup(d => d.GetTotalCostAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(200m);
            _expenseDal.Setup(d => d.GetCategoryTotalsAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<CategoryTotalDto>());

            return new ReportManager(_vehicleAccess.Object, _maintenanceDal.Object, _fuelDal.Object, _expenseDal.Object, _userDal.Object, _companyDal.Object, _evrakDal.Object, _reminderDal.Object, _assignmentDal.Object, _receiptDraftDal.Object, TestPlanKurallari.Olustur(), _lastikDal.Object, TestEvrakKurallari.Olustur(), Mock.Of<IHasarDosyasiDal>());
        }

        [Fact]
        public async Task GetSummaryAsync_BitisBaslangictanKucukseInvalidValueDoner()
        {
            var result = await CreateManager().GetSummaryAsync(UserId, VehicleId, new DateTime(2026, 5, 1), new DateTime(2026, 4, 30));

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
        }

        [Fact]
        public async Task GetSummaryAsync_AyniGunAraligiCalisir()
        {
            var gun = new DateTime(2026, 5, 1);

            var result = await CreateManager().GetSummaryAsync(UserId, VehicleId, gun, gun);

            Assert.True(result.Success);
            Assert.Equal(300m, result.Data.GrandTotal);
        }

        [Fact]
        public async Task GetSummaryAsync_AyniGunAraliginda_BitisGunSonunaKadarGenisletilir()
        {
            DateTime kullanilanBitis = default;
            _vehicleAccess.Setup(d => d.GetAccessibleAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new Vehicle { Id = VehicleId, UserId = UserId, Plate = "34ABC123" });
            _fuelDal.Setup(d => d.GetTotalCostAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Callback<int, DateTime, DateTime>((_, __, end) => kullanilanBitis = end)
                .ReturnsAsync(0m);
            _maintenanceDal.Setup(d => d.GetTotalCostAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(0m);
            _expenseDal.Setup(d => d.GetCategoryTotalsAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<CategoryTotalDto>());

            var manager = new ReportManager(_vehicleAccess.Object, _maintenanceDal.Object, _fuelDal.Object, _expenseDal.Object, _userDal.Object, _companyDal.Object, _evrakDal.Object, _reminderDal.Object, _assignmentDal.Object, _receiptDraftDal.Object, TestPlanKurallari.Olustur(), _lastikDal.Object, TestEvrakKurallari.Olustur(), Mock.Of<IHasarDosyasiDal>());
            await manager.GetSummaryAsync(UserId, VehicleId, new DateTime(2026, 5, 1), new DateTime(2026, 5, 1));

            Assert.Equal(new DateTime(2026, 5, 1, 23, 59, 59), kullanilanBitis, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetSummaryAsync_EnBuyukTarihIleCagrildigindaPatlamaz()
        {
            var result = await CreateManager().GetSummaryAsync(UserId, VehicleId, new DateTime(2026, 1, 1), DateTime.MaxValue.Date);

            Assert.True(result.Success);
            Assert.Equal(300m, result.Data.GrandTotal);
        }
    }
}
