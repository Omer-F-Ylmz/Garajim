using System.Linq.Expressions;
using Garajim.Business.Jobs;
using Garajim.Core.Multitenancy;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Moq;

namespace Garajim.Tests.Integration
{
    public class Denetim2JobDayaniklilikTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private static List<Company> UcSirket()
        {
            return new List<Company>
            {
                new Company { Id = 1, Name = "Bir" },
                new Company { Id = 2, Name = "Patlayan" },
                new Company { Id = 3, Name = "Uc" }
            };
        }

        [Fact]
        public async Task OzetJobundaBirSirketinHatasiDigerleriniDurdurmaz()
        {
            var companyDal = new Mock<ICompanyDal>();
            companyDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<Company, bool>>>())).ReturnsAsync(UcSirket());

            var mesajDal = new Mock<IUstaMesajDal>();
            var gorulen = new List<int>();
            var tenant = new TenantContext();

            mesajDal.Setup(d => d.GetOzetlenmemisCozumluMesajlarAsync())
                .Returns(() =>
                {
                    gorulen.Add(tenant.CompanyId ?? 0);
                    if (tenant.CompanyId == 2)
                    {
                        throw new InvalidOperationException("bu sirkette bozuk kayit");
                    }
                    return Task.FromResult(new List<UstaMesaj>());
                });

            var job = new UstaOzetJob(companyDal.Object, mesajDal.Object, _db.UstaSohbetDal, _db.VehicleDal,
                _db.PartDal, _db.UstaCozumOzetiDal, _db.UserDal, _db.UnitOfWork, tenant);

            var hata = await Record.ExceptionAsync(() => job.RunAsync());

            Assert.Null(hata);
            Assert.Equal(new[] { 1, 2, 3 }, gorulen);
            Assert.Null(tenant.CompanyId);
        }

        [Fact]
        public async Task SaklamaJobundaBirSirketinHatasiDigerleriniDurdurmaz()
        {
            var companyDal = new Mock<ICompanyDal>();
            companyDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<Company, bool>>>())).ReturnsAsync(UcSirket());

            var sohbetDal = new Mock<IUstaSohbetDal>();
            var mesajDal = new Mock<IUstaMesajDal>();
            var gorulen = new List<int>();
            var tenant = new TenantContext();

            mesajDal.Setup(d => d.GetOzetlenmemisCozumluMesajlarAsync()).ReturnsAsync(new List<UstaMesaj>());

            sohbetDal.Setup(d => d.EskiSohbetIdleriAsync(It.IsAny<DateTime>()))
                .Returns(() =>
                {
                    gorulen.Add(tenant.CompanyId ?? 0);
                    if (tenant.CompanyId == 2)
                    {
                        throw new InvalidOperationException("bu sirkette silme hatasi");
                    }
                    return Task.FromResult(new List<int>());
                });

            var ozetJob = new UstaOzetJob(companyDal.Object, mesajDal.Object, sohbetDal.Object, _db.VehicleDal,
                _db.PartDal, _db.UstaCozumOzetiDal, _db.UserDal, _db.UnitOfWork, tenant);
            var job = new UstaSaklamaJob(companyDal.Object, sohbetDal.Object, mesajDal.Object, ozetJob, tenant);

            var hata = await Record.ExceptionAsync(() => job.RunAsync());

            Assert.Null(hata);
            Assert.Equal(new[] { 1, 2, 3 }, gorulen);
            Assert.Null(tenant.CompanyId);
        }

        [Fact]
        public async Task HataSonrasiKiraciBaglamiTemizlenir()
        {
            var companyDal = new Mock<ICompanyDal>();
            companyDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<Company, bool>>>())).ReturnsAsync(UcSirket());

            var mesajDal = new Mock<IUstaMesajDal>();
            var tenant = new TenantContext();

            mesajDal.Setup(d => d.GetOzetlenmemisCozumluMesajlarAsync())
                .Returns(() => throw new InvalidOperationException("her sirkette patliyor"));

            var job = new UstaOzetJob(companyDal.Object, mesajDal.Object, _db.UstaSohbetDal, _db.VehicleDal,
                _db.PartDal, _db.UstaCozumOzetiDal, _db.UserDal, _db.UnitOfWork, tenant);

            await job.RunAsync();

            Assert.Null(tenant.CompanyId);
        }

        public void Dispose() => _db.Dispose();
    }
}
