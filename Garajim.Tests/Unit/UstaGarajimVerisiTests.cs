using Garajim.Business.Abstract;
using System.Linq.Expressions;
using Garajim.Business.Concrete;
using Garajim.Business.Usta;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Garajim.Tests.Integration;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Garajim.Tests.Unit
{
    public class UstaGarajimVerisiTests
    {
        private const int UserId = 3;
        private const int CompanyId = 9;
        private const int VehicleId = 11;

        private readonly Mock<IUstaSohbetDal> _sohbetDal = new Mock<IUstaSohbetDal>();
        private readonly Mock<IUstaMesajDal> _mesajDal = new Mock<IUstaMesajDal>();
        private readonly Mock<IUstaOnayDal> _onayDal = new Mock<IUstaOnayDal>();
        private readonly Mock<IUstaCozumOzetiDal> _ozetDal = new Mock<IUstaCozumOzetiDal>();
        private readonly Mock<IUserDal> _userDal = new Mock<IUserDal>();
        private readonly Mock<ICompanyDal> _companyDal = new Mock<ICompanyDal>();
        private readonly Mock<Garajim.Business.Abstract.IVehicleAccessService> _vehicleAccess = new Mock<Garajim.Business.Abstract.IVehicleAccessService>();
        private readonly Mock<IMaintenanceDal> _maintenanceDal = new Mock<IMaintenanceDal>();
        private readonly Mock<IMaintenancePartDal> _partDal = new Mock<IMaintenancePartDal>();
        private readonly Mock<Garajim.Business.Abstract.IPartMemoryService> _partMemory = new Mock<Garajim.Business.Abstract.IPartMemoryService>();
        private readonly Mock<IEvrakDal> _evrakDal = new Mock<IEvrakDal>();
        private readonly Mock<IFuelDal> _fuelDal = new Mock<IFuelDal>();
        private readonly Mock<IReminderDal> _reminderDal = new Mock<IReminderDal>();
        private readonly SahteUstaIstemci _istemci = new SahteUstaIstemci();

        private UstaManager Yonetici(bool garajimVerisi, List<UstaCozumOzeti> ozetler)
        {
            _userDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<AppUser, bool>>>()))
                .ReturnsAsync(new AppUser { Id = UserId, CompanyId = CompanyId, Role = CompanyRole.Owner });
            _companyDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Company, bool>>>()))
                .ReturnsAsync(new Company { Id = CompanyId, Name = "Test", PlanType = PlanType.Bireysel });
            _onayDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<UstaOnay, bool>>>()))
                .ReturnsAsync(new UstaOnay { Id = 1, UserId = UserId, MetinSurumu = UstaManager.VarsayilanOnaySurumu });
            _sohbetDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<UstaSohbet, bool>>>()))
                .ReturnsAsync(new UstaSohbet { Id = 5, CompanyId = CompanyId, VehicleId = VehicleId, UserId = UserId });
            _vehicleAccess.Setup(d => d.GetAccessibleAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new Vehicle { Id = VehicleId, CompanyId = CompanyId, Plate = "34AAA111", Brand = "Renault", Model = "Clio", Year = 2019, CurrentKm = 120000 });
            _mesajDal.Setup(d => d.KullaniciGunlukSayisiAsync(It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync(0);
            _mesajDal.Setup(d => d.SohbetMesajSayisiAsync(It.IsAny<int>())).ReturnsAsync(0);
            _mesajDal.Setup(d => d.GetSohbetMesajlariAsync(It.IsAny<int>())).ReturnsAsync(new List<UstaMesaj>());
            _mesajDal.Setup(d => d.AddAsync(It.IsAny<UstaMesaj>())).Returns(Task.CompletedTask);
            _maintenanceDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<MaintenanceRecord, bool>>>())).ReturnsAsync(new List<MaintenanceRecord>());
            _partDal.Setup(d => d.GetByVehicleAsync(It.IsAny<int>())).ReturnsAsync(new List<MaintenancePart>());
            _partMemory.Setup(d => d.GetAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new Garajim.Core.Utilities.Results.SuccessDataResult<List<ParcaHafizasiDto>>(new List<ParcaHafizasiDto>()));
            _evrakDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<EvrakKaydi, bool>>>())).ReturnsAsync(new List<EvrakKaydi>());
            _fuelDal.Setup(d => d.GetOlcumlerAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<YakitOlcumDto>());
            _reminderDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<Reminder, bool>>>())).ReturnsAsync(new List<Reminder>());
            _ozetDal.Setup(d => d.GetTumuAsync()).ReturnsAsync(ozetler);

            var yapilandirma = new ConfigurationBuilder()
                .AddInMemoryCollection(new[] { new KeyValuePair<string, string>("Usta:GarajimVerisi", garajimVerisi ? "true" : "false") })
                .Build();

            var kayitlar = new BilgiYukleyici().Yukle(Path.Combine(AppContext.BaseDirectory, BilgiYukleyici.KlasorAdi));
            var depo = new UstaBilgiDeposu(kayitlar, "SISTEM");

            return new UstaManager(_sohbetDal.Object, _mesajDal.Object, _onayDal.Object, _ozetDal.Object,
                _userDal.Object, _companyDal.Object, _vehicleAccess.Object, _maintenanceDal.Object, _partDal.Object,
                _partMemory.Object, _evrakDal.Object, _fuelDal.Object, new Mock<IAiButcesi>().Object, _reminderDal.Object, _istemci, depo,
                TestEvrakKurallari.Olustur(), yapilandirma);
        }

        private static UstaCozumOzeti Ozet(int sayi, string marka = "Renault", string model = "Clio")
        {
            return new UstaCozumOzeti
            {
                Marka = marka,
                Model = model,
                BelirtiKategori = "Fren",
                ParcaTuru = "FrenBalatasiOn",
                Sayi = sayi,
                GuncellemeTarihi = DateTime.UtcNow
            };
        }

        private async Task<string> SabitBlokAsync(bool garajimVerisi, List<UstaCozumOzeti> ozetler)
        {
            var yonetici = Yonetici(garajimVerisi, ozetler);
            await yonetici.MesajGonderAsync(UserId, 5, new UstaMesajGonderDto { Metin = "frende ses var" }, CancellationToken.None);
            return _istemci.Cagrilar.Last().SabitBlok;
        }

        [Fact]
        public async Task BayrakKapaliykenGarajimVerisiPromptaGirmez()
        {
            var blok = await SabitBlokAsync(false, new List<UstaCozumOzeti> { Ozet(100) });

            Assert.DoesNotContain("GARAJIM VERISI", blok);
        }

        [Fact]
        public async Task EsikAltiSatirPromptaGirmez()
        {
            var blok = await SabitBlokAsync(true, new List<UstaCozumOzeti> { Ozet(UstaManager.GarajimVerisiEsigi - 1) });

            Assert.DoesNotContain("GARAJIM VERISI", blok);
        }

        [Fact]
        public async Task EsikUstuSatirNSayisiylaPromptaGirer()
        {
            var blok = await SabitBlokAsync(true, new List<UstaCozumOzeti> { Ozet(UstaManager.GarajimVerisiEsigi) });

            Assert.Contains("GARAJIM VERISI", blok);
            Assert.Contains("n=30", blok);
            Assert.Contains("FrenBalatasiOn", blok);
        }

        [Fact]
        public async Task BaskaMarkaninSatiriPromptaGirmez()
        {
            var blok = await SabitBlokAsync(true, new List<UstaCozumOzeti> { Ozet(90, "Ford", "Focus") });

            Assert.DoesNotContain("GARAJIM VERISI", blok);
        }

        [Fact]
        public async Task GarajimVerisiBlogundaSirketVeKullaniciBilgisiYok()
        {
            var blok = await SabitBlokAsync(true, new List<UstaCozumOzeti> { Ozet(45) });

            var satirlar = blok.Split('\n').SkipWhile(s => !s.StartsWith("GARAJIM VERISI")).ToList();

            Assert.NotEmpty(satirlar);
            Assert.DoesNotContain(satirlar, s => s.Contains("CompanyId") || s.Contains("34AAA111") || s.Contains("@"));
        }
    }
}
