using System.Linq.Expressions;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Business.Katalog;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Moq;

namespace Garajim.Tests.Unit
{
    public class GlobalSeriTahminTests : IDisposable
    {
        private const int UserId = 4;
        private const int VehicleId = 12;

        private readonly string _klasor;
        private readonly AracKatalogu _katalog;
        private readonly Mock<IAracDegerDal> _degerDal = new Mock<IAracDegerDal>();
        private readonly Mock<IVehicleAccessService> _vehicleAccess = new Mock<IVehicleAccessService>();
        private readonly Mock<IDegerTahminEdici> _tahminEdici = new Mock<IDegerTahminEdici>();

        public GlobalSeriTahminTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-deger-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_klasor);

            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            File.Copy(
                Path.Combine(kok.FullName, "Garajim.Business", "Katalog", AracKatalogu.DosyaAdi),
                Path.Combine(_klasor, AracKatalogu.DosyaAdi));

            File.WriteAllText(Path.Combine(_klasor, AracKatalogu.GlobalDosyaAdi), @"{
  ""surum"": ""test-g1"", ""kaynak"": ""test"", ""uretimTarihi"": ""2026-09-08"",
  ""markalar"": [
    { ""ad"": ""Tesla"", ""tr"": true, ""seriler"": [ { ""ad"": ""Cybertruck"", ""tr"": false } ] },
    { ""ad"": ""Land Rover"", ""tr"": false, ""seriler"": [ { ""ad"": ""Defender"", ""tr"": false } ] }
  ]
}");

            _katalog = AracKatalogu.Yukle(_klasor);
        }

        public void Dispose()
        {
            if (Directory.Exists(_klasor))
            {
                Directory.Delete(_klasor, true);
            }
        }

        private DegerManager Yonetici(string marka, string model)
        {
            _vehicleAccess.Setup(s => s.GetAccessibleAsync(UserId, VehicleId))
                .ReturnsAsync(new Vehicle
                {
                    Id = VehicleId,
                    Brand = marka,
                    Model = model,
                    Year = 2022,
                    CurrentKm = 40000,
                    FuelType = FuelType.Benzin,
                    Vites = "Otomatik",
                    KasaTipi = KasaTipi.Sedan,
                    ModelEslesmedi = false
                });

            _degerDal.Setup(d => d.GunlukTahminSayisiAsync(VehicleId, It.IsAny<DateTime>())).ReturnsAsync(0);

            return new DegerManager(_degerDal.Object, _vehicleAccess.Object, _tahminEdici.Object, _katalog);
        }

        [Fact]
        public async Task GlobalSeriTahminiReddedilir()
        {
            var sonuc = await Yonetici("Tesla", "Cybertruck").TahminAsync(UserId, VehicleId);

            Assert.False(sonuc.Success);
            Assert.Equal(Messages.DegerGlobalSeri, sonuc.Message);
        }

        [Fact]
        public async Task GlobalMarkaTahminiReddedilir()
        {
            var sonuc = await Yonetici("Land Rover", "Defender").TahminAsync(UserId, VehicleId);

            Assert.False(sonuc.Success);
            Assert.Equal(Messages.DegerGlobalSeri, sonuc.Message);
        }

        [Fact]
        public async Task TrSeriGlobalKapisindaTakilmaz()
        {
            var sonuc = await Yonetici("Tesla", "Model 3").TahminAsync(UserId, VehicleId);

            Assert.NotEqual(Messages.DegerGlobalSeri, sonuc.Message);
        }
    }
}
