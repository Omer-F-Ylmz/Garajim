using System.Linq.Expressions;
using Garajim.Business.Jobs;
using Garajim.Business.Katalog;
using Garajim.Core.Multitenancy;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Moq;

namespace Garajim.Tests.Unit
{
    public class GlobalKatalogEslemeTests : IDisposable
    {
        private readonly string _klasor;

        public GlobalKatalogEslemeTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-esleme-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_klasor);

            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            File.Copy(
                Path.Combine(kok.FullName, "Garajim.Business", "Katalog", AracKatalogu.DosyaAdi),
                Path.Combine(_klasor, AracKatalogu.DosyaAdi));
        }

        public void Dispose()
        {
            if (Directory.Exists(_klasor))
            {
                Directory.Delete(_klasor, true);
            }
        }

        private void GlobalYaz() => File.WriteAllText(Path.Combine(_klasor, AracKatalogu.GlobalDosyaAdi), @"{
  ""surum"": ""test-g1"", ""kaynak"": ""test"", ""uretimTarihi"": ""2026-09-08"",
  ""markalar"": [ { ""ad"": ""Land Rover"", ""tr"": false, ""seriler"": [ { ""ad"": ""Defender"", ""tr"": false } ] } ]
}");

        private static (KatalogEslemeJob Job, List<Vehicle> Araclar) Kur(AracKatalogu katalog)
        {
            var araclar = new List<Vehicle>
            {
                new Vehicle
                {
                    Id = 1,
                    CompanyId = 7,
                    Plate = "34ABC123",
                    Brand = "Land Rover",
                    Model = "Defender",
                    Year = 2018,
                    CurrentKm = 90000,
                    FuelType = FuelType.Dizel,
                    ModelEslesmedi = true
                }
            };

            var companyDal = new Mock<ICompanyDal>();
            companyDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<Company, bool>>>()))
                .ReturnsAsync(new List<Company> { new Company { Id = 7, Name = "Test" } });

            var vehicleDal = new Mock<IVehicleDal>();
            vehicleDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(araclar);
            vehicleDal.Setup(d => d.UpdateAsync(It.IsAny<Vehicle>())).Returns(Task.CompletedTask);

            return (new KatalogEslemeJob(companyDal.Object, vehicleDal.Object, katalog, new TenantContext()), araclar);
        }

        [Fact]
        public async Task GlobalDosyaYokkenEslesmemisAracOyleKalir()
        {
            var (job, araclar) = Kur(AracKatalogu.Yukle(_klasor));

            await job.RunAsync();

            Assert.True(araclar[0].ModelEslesmedi);
        }

        [Fact]
        public async Task GlobalDosyaVarkenEslesmemisAracCozulur()
        {
            GlobalYaz();

            var (job, araclar) = Kur(AracKatalogu.Yukle(_klasor));

            var degisen = await job.RunAsync();

            Assert.Equal(1, degisen);
            Assert.False(araclar[0].ModelEslesmedi);
            Assert.Equal("Land Rover", araclar[0].Brand);
            Assert.Equal("Defender", araclar[0].Model);
        }

        [Fact]
        public async Task GlobalEslemeIkinciKosudaSifirDoner()
        {
            GlobalYaz();

            var (job, _) = Kur(AracKatalogu.Yukle(_klasor));

            await job.RunAsync();

            Assert.Equal(0, await job.RunAsync());
        }
    }
}
