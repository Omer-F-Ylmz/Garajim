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
    public class VehicleManagerTests
    {
        private const int UserId = 7;

        private readonly Mock<IVehicleDal> _vehicleDal = new Mock<IVehicleDal>();
        private readonly Mock<IUserDal> _userDal = new Mock<IUserDal>();

        private VehicleManager CreateManager()
        {
            _userDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<AppUser, bool>>>()))
                .ReturnsAsync(new AppUser { Id = UserId, CompanyId = 42 });
            return new VehicleManager(_vehicleDal.Object, _userDal.Object);
        }

        private static VehicleCreateDto ValidDto()
        {
            return new VehicleCreateDto
            {
                Plate = "34ABC123",
                Brand = "Renault",
                Model = "Clio",
                Year = 2018,
                CurrentKm = 120000,
                FuelType = FuelType.Benzin
            };
        }

        [Theory]
        [InlineData(" 34 abc 123 ", "34ABC123")]
        [InlineData("06 xy 9876", "06XY9876")]
        [InlineData("35bz1", "35BZ1")]
        public async Task AddAsync_PlakaBuyukHarfeCevrilipBosluklarSilinir(string girilen, string beklenen)
        {
            Vehicle eklenen = null;
            _vehicleDal.Setup(d => d.AnyAsync(It.IsAny<Expression<Func<Vehicle, bool>>>())).ReturnsAsync(false);
            _vehicleDal.Setup(d => d.AddAsync(It.IsAny<Vehicle>()))
                .Callback<Vehicle>(vehicle => eklenen = vehicle)
                .Returns(Task.CompletedTask);

            var dto = ValidDto();
            dto.Plate = girilen;

            var result = await CreateManager().AddAsync(UserId, dto);

            Assert.True(result.Success);
            Assert.Equal(beklenen, eklenen.Plate);
            Assert.Equal(beklenen, result.Data.Plate);
            Assert.Equal(UserId, eklenen.UserId);
        }

        [Fact]
        public async Task AddAsync_AyniKullanicidaAyniPlakaIkinciKezEklenemez()
        {
            _vehicleDal.Setup(d => d.AnyAsync(It.IsAny<Expression<Func<Vehicle, bool>>>())).ReturnsAsync(true);

            var result = await CreateManager().AddAsync(UserId, ValidDto());

            Assert.False(result.Success);
            Assert.Equal(Messages.PlateAlreadyExists, result.Message);
            _vehicleDal.Verify(d => d.AddAsync(It.IsAny<Vehicle>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_PlakaTekilligiSadeceAyniKullaniciIcindir()
        {
            Expression<Func<Vehicle, bool>> mukerrerKontrolu = null;
            _vehicleDal.Setup(d => d.AnyAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .Callback<Expression<Func<Vehicle, bool>>>(predicate => mukerrerKontrolu = predicate)
                .ReturnsAsync(false);
            _vehicleDal.Setup(d => d.AddAsync(It.IsAny<Vehicle>())).Returns(Task.CompletedTask);

            var dto = ValidDto();
            dto.Plate = " 34 abc 123 ";

            await CreateManager().AddAsync(UserId, dto);

            var kontrol = mukerrerKontrolu.Compile();
            Assert.True(kontrol(new Vehicle { UserId = UserId, Plate = "34ABC123" }));
            Assert.False(kontrol(new Vehicle { UserId = UserId + 1, Plate = "34ABC123" }));
            Assert.False(kontrol(new Vehicle { UserId = UserId, Plate = "34ABC124" }));
        }

        [Theory]
        [InlineData(1949, false)]
        [InlineData(1950, true)]
        [InlineData(2000, true)]
        public async Task AddAsync_YilAltSinirindaDogrulanir(int yil, bool basariliOlmali)
        {
            _vehicleDal.Setup(d => d.AnyAsync(It.IsAny<Expression<Func<Vehicle, bool>>>())).ReturnsAsync(false);
            _vehicleDal.Setup(d => d.AddAsync(It.IsAny<Vehicle>())).Returns(Task.CompletedTask);

            var dto = ValidDto();
            dto.Year = yil;

            var result = await CreateManager().AddAsync(UserId, dto);

            Assert.Equal(basariliOlmali, result.Success);
            if (!basariliOlmali)
                Assert.Equal(Messages.InvalidValue, result.Message);
        }

        [Fact]
        public async Task AddAsync_GelecekYilArtiBirKabulEdilirIkiReddedilir()
        {
            _vehicleDal.Setup(d => d.AnyAsync(It.IsAny<Expression<Func<Vehicle, bool>>>())).ReturnsAsync(false);
            _vehicleDal.Setup(d => d.AddAsync(It.IsAny<Vehicle>())).Returns(Task.CompletedTask);

            var sinir = DateTime.UtcNow.Year + 1;

            var dtoSinir = ValidDto();
            dtoSinir.Year = sinir;
            var sinirSonucu = await CreateManager().AddAsync(UserId, dtoSinir);

            var dtoAsan = ValidDto();
            dtoAsan.Year = sinir + 1;
            var asanSonuc = await CreateManager().AddAsync(UserId, dtoAsan);

            Assert.True(sinirSonucu.Success);
            Assert.False(asanSonuc.Success);
            Assert.Equal(Messages.InvalidValue, asanSonuc.Message);
        }

        [Fact]
        public async Task AddAsync_NegatifKilometreReddedilir()
        {
            var dto = ValidDto();
            dto.CurrentKm = -1;

            var result = await CreateManager().AddAsync(UserId, dto);

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            _vehicleDal.Verify(d => d.AddAsync(It.IsAny<Vehicle>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddAsync_BosPlakaReddedilir(string plaka)
        {
            var dto = ValidDto();
            dto.Plate = plaka;

            var result = await CreateManager().AddAsync(UserId, dto);

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
        }

        [Fact]
        public async Task UpdateAsync_GecersizYilKaydiDegistirmez()
        {
            var vehicle = new Vehicle { Id = 1, UserId = UserId, Plate = "34ABC123", Brand = "Renault", Model = "Clio", Year = 2018, CurrentKm = 100000 };
            _vehicleDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Vehicle, bool>>>())).ReturnsAsync(vehicle);

            var result = await CreateManager().UpdateAsync(UserId, 1, new VehicleUpdateDto
            {
                Brand = "Renault",
                Model = "Clio",
                Year = 1949,
                CurrentKm = 130000
            });

            Assert.False(result.Success);
            Assert.Equal(Messages.InvalidValue, result.Message);
            Assert.Equal(2018, vehicle.Year);
            Assert.Equal(100000, vehicle.CurrentKm);
            _vehicleDal.Verify(d => d.UpdateAsync(It.IsAny<Vehicle>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_BaskaKullanicininAraciBulunamazDoner()
        {
            _vehicleDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Vehicle, bool>>>())).ReturnsAsync((Vehicle)null);

            var result = await CreateManager().GetByIdAsync(UserId, 1);

            Assert.False(result.Success);
            Assert.Equal(Messages.VehicleNotFound, result.Message);
        }
    }
}
