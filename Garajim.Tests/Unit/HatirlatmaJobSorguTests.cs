using System.Linq.Expressions;
using Garajim.Business.Abstract;
using Garajim.Business.Jobs;
using Garajim.Core.Multitenancy;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Garajim.Tests.Unit
{
    public class HatirlatmaJobSorguTests
    {
        private readonly Mock<ICompanyDal> _companyDal = new Mock<ICompanyDal>();
        private readonly Mock<IReminderDal> _reminderDal = new Mock<IReminderDal>();
        private readonly Mock<IEvrakDal> _evrakDal = new Mock<IEvrakDal>();
        private readonly Mock<IUserDal> _userDal = new Mock<IUserDal>();
        private readonly Mock<IVehicleAssignmentDal> _assignmentDal = new Mock<IVehicleAssignmentDal>();
        private readonly Mock<IEmailSender> _emailSender = new Mock<IEmailSender>();

        private ReminderNotificationJob Job()
        {
            return new ReminderNotificationJob(
                _companyDal.Object,
                _reminderDal.Object,
                new TenantContext(),
                _emailSender.Object,
                new ConfigurationBuilder().Build(),
                _evrakDal.Object,
                _userDal.Object,
                _assignmentDal.Object,
                TestEvrakKurallari.Olustur());
        }

        private void Hazirla(int evrakSayisi, int kullaniciSayisi)
        {
            _companyDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<Company, bool>>>()))
                .ReturnsAsync(new List<Company> { new Company { Id = 1, Name = "Tek Şirket" } });

            _reminderDal.Setup(d => d.GetDueListAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ReminderDueDto>());

            var evraklar = Enumerable.Range(1, evrakSayisi).Select(no => new EvrakDueDto
            {
                EvrakId = no,
                CompanyId = 1,
                VehicleId = no,
                Plate = "34AA" + no,
                EvrakTuru = EvrakTuru.Muayene,
                BitisTarihi = DateTime.UtcNow.Date.AddDays(3)
            }).ToList();

            _evrakDal.Setup(d => d.GetDueListAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(evraklar);

            _evrakDal.Setup(d => d.TryClaimNotificationAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(true);

            var kullanicilar = Enumerable.Range(1, kullaniciSayisi).Select(no => new AppUser
            {
                Id = no,
                CompanyId = 1,
                Email = $"kisi{no}@ornek.test",
                FullName = "Kişi " + no,
                IsActive = true,
                Role = no == 1 ? CompanyRole.Owner : CompanyRole.Driver
            }).ToList();

            _userDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<AppUser, bool>>>()))
                .ReturnsAsync(kullanicilar);

            _assignmentDal.Setup(d => d.GetActiveByVehicleAsync(It.IsAny<int>()))
                .ReturnsAsync((VehicleAssignment)null);
        }

        [Fact]
        public async Task KullaniciListesiEvrakBasinaDegilSirketBasinaOkunur()
        {
            Hazirla(evrakSayisi: 25, kullaniciSayisi: 10);

            await Job().RunAsync();

            _userDal.Verify(d => d.GetListAsync(It.IsAny<Expression<Func<AppUser, bool>>>()), Times.Once);
        }

        [Fact]
        public async Task EvraklarIcinBildirimYineDeGonderilir()
        {
            Hazirla(evrakSayisi: 3, kullaniciSayisi: 2);

            await Job().RunAsync();

            _emailSender.Verify(
                d => d.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(3));
        }
    }
}
