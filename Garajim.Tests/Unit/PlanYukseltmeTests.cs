using System.Linq.Expressions;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Garajim.Tests.Unit
{
    public class PlanYukseltmeTests
    {
        private const int UserId = 11;
        private const int CompanyId = 42;

        private sealed class SahteEmailSender : IEmailSender
        {
            public List<(string To, string Subject, string Body)> Gonderilenler { get; } = new List<(string, string, string)>();

            public Task SendAsync(string to, string subject, string body)
            {
                Gonderilenler.Add((to, subject, body));
                return Task.CompletedTask;
            }
        }

        private readonly Mock<IUserDal> _userDal = new Mock<IUserDal>();
        private readonly Mock<ICompanyDal> _companyDal = new Mock<ICompanyDal>();
        private readonly Mock<IVehicleDal> _vehicleDal = new Mock<IVehicleDal>();
        private readonly SahteEmailSender _email = new SahteEmailSender();

        private PlanManager Yonetici(string destekEposta = "destek@garajim.app", CompanyRole rol = CompanyRole.Owner, PlanType plan = PlanType.Bireysel)
        {
            _userDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<AppUser, bool>>>()))
                .ReturnsAsync(new AppUser { Id = UserId, CompanyId = CompanyId, Role = rol, FullName = "Ali Veli", Email = "ali@garajim.local" });
            _companyDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Company, bool>>>()))
                .ReturnsAsync(new Company { Id = CompanyId, Name = "Veli Nakliyat", PlanType = plan });
            _companyDal.Setup(d => d.DavetSayisiAsync(It.IsAny<int>())).ReturnsAsync(0);
            _vehicleDal.Setup(d => d.CountAsync(It.IsAny<Expression<Func<Vehicle, bool>>>())).ReturnsAsync(3);

            var yapilandirma = new ConfigurationBuilder()
                .AddInMemoryCollection(new[] { new KeyValuePair<string, string>("App:DestekEposta", destekEposta) })
                .Build();

            return new PlanManager(_userDal.Object, _companyDal.Object, _vehicleDal.Object, _email, TestPlanKurallari.Olustur(), yapilandirma);
        }

        private static PlanYukseltmeTalebiDto Talep(PlanType istenen = PlanType.Filo, string mesaj = "12 aracımız var")
        {
            return new PlanYukseltmeTalebiDto { IstenenPlan = istenen, Mesaj = mesaj };
        }

        [Fact]
        public async Task TalepDestekAdresineEpostaGonderir()
        {
            var sonuc = await Yonetici().YukseltmeTalebiAsync(UserId, Talep());

            Assert.True(sonuc.Success);
            var gonderilen = Assert.Single(_email.Gonderilenler);
            Assert.Equal("destek@garajim.app", gonderilen.To);
            Assert.Contains("Veli Nakliyat", gonderilen.Subject);
            Assert.Contains("Filo", gonderilen.Body);
            Assert.Contains("Bireysel", gonderilen.Body);
            Assert.Contains("ali@garajim.local", gonderilen.Body);
            Assert.Contains("12 aracımız var", gonderilen.Body);
        }

        [Fact]
        public async Task GovdeMevcutAracSayisiVeLimitiTasir()
        {
            await Yonetici().YukseltmeTalebiAsync(UserId, Talep());

            var govde = _email.Gonderilenler[0].Body;

            Assert.Contains("3", govde);
            Assert.Contains("Araç", govde);
        }

        [Fact]
        public async Task DestekAdresiTanimsizsaTalepReddedilir()
        {
            var sonuc = await Yonetici(destekEposta: "").YukseltmeTalebiAsync(UserId, Talep());

            Assert.False(sonuc.Success);
            Assert.Equal(Messages.DestekEpostasiTanimsiz, sonuc.Message);
            Assert.Empty(_email.Gonderilenler);
        }

        [Fact]
        public async Task MevcutPlanIstenirseReddedilir()
        {
            var sonuc = await Yonetici(plan: PlanType.Filo).YukseltmeTalebiAsync(UserId, Talep(PlanType.Filo));

            Assert.False(sonuc.Success);
            Assert.Equal(Messages.PlanZatenAktif, sonuc.Message);
            Assert.Empty(_email.Gonderilenler);
        }

        [Fact]
        public async Task TanimsizPlanReddedilir()
        {
            var sonuc = await Yonetici().YukseltmeTalebiAsync(UserId, Talep((PlanType)9));

            Assert.False(sonuc.Success);
            Assert.Equal(Messages.InvalidValue, sonuc.Message);
            Assert.Empty(_email.Gonderilenler);
        }

        [Fact]
        public async Task DriverTalepAcamaz()
        {
            var sonuc = await Yonetici(rol: CompanyRole.Driver).YukseltmeTalebiAsync(UserId, Talep());

            Assert.False(sonuc.Success);
            Assert.Equal(Messages.AuthorizationDenied, sonuc.Message);
            Assert.Empty(_email.Gonderilenler);
        }

        [Fact]
        public async Task ManagerTalepAcamaz()
        {
            var sonuc = await Yonetici(rol: CompanyRole.Manager).YukseltmeTalebiAsync(UserId, Talep());

            Assert.False(sonuc.Success);
            Assert.Equal(Messages.AuthorizationDenied, sonuc.Message);
        }

        [Fact]
        public async Task MesajBossaTalepYineGonderilir()
        {
            var sonuc = await Yonetici().YukseltmeTalebiAsync(UserId, Talep(mesaj: null));

            Assert.True(sonuc.Success);
            Assert.Single(_email.Gonderilenler);
        }

        [Fact]
        public async Task CokUzunMesajKirpilir()
        {
            await Yonetici().YukseltmeTalebiAsync(UserId, Talep(mesaj: new string('x', 3000)));

            Assert.DoesNotContain(new string('x', 1001), _email.Gonderilenler[0].Body);
        }
    }
}
