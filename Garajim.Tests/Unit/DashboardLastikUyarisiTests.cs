using System.Linq.Expressions;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete;
using Garajim.Business.Concrete.Evraklar;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Garajim.Tests.Unit
{
    public class DashboardLastikUyarisiTests
    {
        private const int UserId = 5;
        private const int CompanyId = 42;

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

        private static string TumYilPenceresi()
        {
            var bugun = DateTime.UtcNow.Date;
            var bas = bugun.AddDays(-3);
            var son = bugun.AddDays(3);
            return $"{bas:dd-MM}..{son:dd-MM}";
        }

        private static string TumYilPenceresiMetni()
        {
            var bugun = DateTime.UtcNow.Date;
            return Ay(bugun.AddDays(-3)) + "\u2013" + Ay(bugun.AddDays(3));
        }

        private static string Ay(DateTime tarih)
        {
            var kisaltmalar = new[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };
            return tarih.Day + " " + kisaltmalar[tarih.Month - 1];
        }

        private static string PencereDisi()

        {
            var bugun = DateTime.UtcNow.Date;
            var bas = bugun.AddDays(20);
            var son = bugun.AddDays(30);
            return $"{bas:dd-MM}..{son:dd-MM}";
        }

        private ReportManager Yonetici(string pencere, List<Vehicle> araclar, List<LastikSeti> takililar)
        {
            _userDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<AppUser, bool>>>()))
                .ReturnsAsync(new AppUser { Id = UserId, CompanyId = CompanyId, Role = CompanyRole.Owner });
            _companyDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Company, bool>>>()))
                .ReturnsAsync(new Company { Id = CompanyId, Name = "Test", PlanType = PlanType.Filo });
            _companyDal.Setup(d => d.DavetSayisiAsync(It.IsAny<int>())).ReturnsAsync(0);
            _vehicleAccess.Setup(d => d.GetAccessibleListAsync(It.IsAny<int>())).ReturnsAsync(araclar);
            _assignmentDal.Setup(d => d.AktifSayiAsync(It.IsAny<List<int>>())).ReturnsAsync(0);
            _evrakDal.Setup(d => d.DurumSayilariAsync(It.IsAny<List<int>>(), It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync((0, 0));
            _reminderDal.Setup(d => d.YaklasanSayisiAsync(It.IsAny<List<int>>(), It.IsAny<DateTime>())).ReturnsAsync(0);
            _receiptDraftDal.Setup(d => d.BekleyenSayisiAsync()).ReturnsAsync(0);
            _fuelDal.Setup(d => d.GetTotalsByVehicleAsync(It.IsAny<List<int>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Garajim.Entity.Dtos.AracToplamDto>());
            _maintenanceDal.Setup(d => d.GetTotalsByVehicleAsync(It.IsAny<List<int>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Garajim.Entity.Dtos.AracToplamDto>());
            _expenseDal.Setup(d => d.GetTotalsByVehicleAsync(It.IsAny<List<int>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Garajim.Entity.Dtos.AracToplamDto>());
            _lastikDal.Setup(d => d.GetTakiliListeAsync(It.IsAny<List<int>>())).ReturnsAsync(takililar);

            var yapilandirma = new ConfigurationBuilder()
                .AddInMemoryCollection(new[] { new KeyValuePair<string, string>("Evrak:KisLastigi", pencere) })
                .Build();

            return new ReportManager(_vehicleAccess.Object, _maintenanceDal.Object, _fuelDal.Object, _expenseDal.Object,
                _userDal.Object, _companyDal.Object, _evrakDal.Object, _reminderDal.Object, _assignmentDal.Object,
                _receiptDraftDal.Object, TestPlanKurallari.Olustur(), _lastikDal.Object, new EvrakKurallari(yapilandirma), Mock.Of<IHasarDosyasiDal>(), Mock.Of<IDegerService>());
        }

        private static Vehicle Arac(int id, string plaka, KullanimTuru kullanim)
        {
            return new Vehicle { Id = id, CompanyId = CompanyId, Plate = plaka, KullanimTuru = kullanim };
        }

        private static LastikSeti Set(int vehicleId, LastikMevsimi mevsim)
        {
            return new LastikSeti { Id = vehicleId * 10, CompanyId = CompanyId, VehicleId = vehicleId, Mevsim = mevsim, Takili = true, Ad = "Set" };
        }

        [Fact]
        public async Task PencereDisindaUyariYok()
        {
            var yonetici = Yonetici(PencereDisi(),
                new List<Vehicle> { Arac(1, "34AAA111", KullanimTuru.Ticari) },
                new List<LastikSeti>());

            var sonuc = await yonetici.GetDashboardAsync(UserId);

            Assert.False(sonuc.Data.KisLastigiDonemi);
            Assert.Null(sonuc.Data.KisLastigiUyarisi);
            Assert.Empty(sonuc.Data.KisLastigiUyariPlakalari);
        }

        [Fact]
        public async Task PencereIcindeTicariAracKisLastigiYoksaUyarir()
        {
            var yonetici = Yonetici(TumYilPenceresi(),
                new List<Vehicle> { Arac(1, "34AAA111", KullanimTuru.Ticari) },
                new List<LastikSeti>());

            var sonuc = await yonetici.GetDashboardAsync(UserId);

            Assert.True(sonuc.Data.KisLastigiDonemi);
            Assert.NotNull(sonuc.Data.KisLastigiUyarisi);
            Assert.Equal(new[] { "34AAA111" }, sonuc.Data.KisLastigiUyariPlakalari);
        }

        [Fact]
        public async Task PencereIcindeTicariAracYazLastigiVarsaUyarir()
        {
            var yonetici = Yonetici(TumYilPenceresi(),
                new List<Vehicle> { Arac(1, "34AAA111", KullanimTuru.Ticari) },
                new List<LastikSeti> { Set(1, LastikMevsimi.Yaz) });

            var sonuc = await yonetici.GetDashboardAsync(UserId);

            Assert.Equal(new[] { "34AAA111" }, sonuc.Data.KisLastigiUyariPlakalari);
        }

        [Fact]
        public async Task KisLastigiTakiliTicariAracUyarmaz()
        {
            var yonetici = Yonetici(TumYilPenceresi(),
                new List<Vehicle> { Arac(1, "34AAA111", KullanimTuru.Ticari) },
                new List<LastikSeti> { Set(1, LastikMevsimi.Kis) });

            var sonuc = await yonetici.GetDashboardAsync(UserId);

            Assert.True(sonuc.Data.KisLastigiDonemi);
            Assert.Null(sonuc.Data.KisLastigiUyarisi);
            Assert.Empty(sonuc.Data.KisLastigiUyariPlakalari);
        }

        [Fact]
        public async Task DortMevsimSetiTicariAracIcinYeterli()
        {
            var yonetici = Yonetici(TumYilPenceresi(),
                new List<Vehicle> { Arac(1, "34AAA111", KullanimTuru.Ticari) },
                new List<LastikSeti> { Set(1, LastikMevsimi.DortMevsim) });

            var sonuc = await yonetici.GetDashboardAsync(UserId);

            Assert.True(sonuc.Data.KisLastigiDonemi);
            Assert.Null(sonuc.Data.KisLastigiUyarisi);
            Assert.Empty(sonuc.Data.KisLastigiUyariPlakalari);
        }

        [Fact]
        public async Task UyariMetniPencereyiVeMSNotunuTasir()
        {
            var yonetici = Yonetici(TumYilPenceresi(),
                new List<Vehicle> { Arac(1, "34AAA111", KullanimTuru.Ticari) },
                new List<LastikSeti> { Set(1, LastikMevsimi.Yaz) });

            var sonuc = await yonetici.GetDashboardAsync(UserId);

            Assert.Contains(TumYilPenceresiMetni(), sonuc.Data.KisLastigiUyarisi);
            Assert.Contains("M+S", sonuc.Data.KisLastigiUyarisi);
            Assert.Contains("Ticari", sonuc.Data.KisLastigiUyarisi);
        }



        [Fact]
        public async Task HususiAracUyariyaGirmez()
        {
            var yonetici = Yonetici(TumYilPenceresi(),
                new List<Vehicle> { Arac(1, "34AAA111", KullanimTuru.Hususi) },
                new List<LastikSeti>());

            var sonuc = await yonetici.GetDashboardAsync(UserId);

            Assert.True(sonuc.Data.KisLastigiDonemi);
            Assert.Empty(sonuc.Data.KisLastigiUyariPlakalari);
        }

        [Fact]
        public async Task YalnizEksikTicariAraclarListelenir()
        {
            var yonetici = Yonetici(TumYilPenceresi(),
                new List<Vehicle>
                {
                    Arac(1, "34AAA111", KullanimTuru.Ticari),
                    Arac(2, "06BBB222", KullanimTuru.Ticari),
                    Arac(3, "35CCC333", KullanimTuru.Hususi)
                },
                new List<LastikSeti> { Set(2, LastikMevsimi.Kis) });

            var sonuc = await yonetici.GetDashboardAsync(UserId);

            Assert.Equal(new[] { "34AAA111" }, sonuc.Data.KisLastigiUyariPlakalari);
        }

        [Fact]
        public async Task AracYokkenUyariSorgusuCalismaz()
        {
            var yonetici = Yonetici(TumYilPenceresi(), new List<Vehicle>(), new List<LastikSeti>());

            var sonuc = await yonetici.GetDashboardAsync(UserId);

            Assert.Empty(sonuc.Data.KisLastigiUyariPlakalari);
            _lastikDal.Verify(d => d.GetTakiliListeAsync(It.IsAny<List<int>>()), Times.Never);
        }
    }
}
