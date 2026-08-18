using Garajim.Business.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Integration
{
    public class CurrentKmUpdateTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        [Fact]
        public async Task MaintenanceAdd_DahaYuksekKilometreAracaYazilir()
        {
            var userId = _db.KullaniciEkle("a@garajim.local").Id;
            var arac = _db.AracEkle(userId, "34AAA111", 100000);
            var manager = new MaintenanceManager(_db.MaintenanceDal, _db.VehicleDal);

            var result = await manager.AddAsync(userId, new MaintenanceCreateDto
            {
                VehicleId = arac.Id,
                Type = MaintenanceType.PeriyodikBakim,
                Date = new DateTime(2026, 3, 1),
                Km = 120000,
                Cost = 4500m
            });

            Assert.True(result.Success);
            Assert.Equal(120000, _db.AraciYenidenOku(arac.Id).CurrentKm);
        }

        [Fact]
        public async Task MaintenanceAdd_DahaDusukKilometreAracaYazilmaz()
        {
            var userId = _db.KullaniciEkle("a@garajim.local").Id;
            var arac = _db.AracEkle(userId, "34AAA111", 100000);
            var manager = new MaintenanceManager(_db.MaintenanceDal, _db.VehicleDal);

            var result = await manager.AddAsync(userId, new MaintenanceCreateDto
            {
                VehicleId = arac.Id,
                Type = MaintenanceType.YagDegisimi,
                Date = new DateTime(2026, 2, 1),
                Km = 90000,
                Cost = 2000m
            });

            Assert.True(result.Success);
            Assert.Equal(100000, _db.AraciYenidenOku(arac.Id).CurrentKm);
        }

        [Fact]
        public async Task FuelAdd_DahaYuksekKilometreAracaYazilir()
        {
            var userId = _db.KullaniciEkle("a@garajim.local").Id;
            var arac = _db.AracEkle(userId, "34AAA111", 100000);
            var manager = new FuelManager(_db.FuelDal, _db.VehicleDal);

            var result = await manager.AddAsync(userId, new FuelCreateDto
            {
                VehicleId = arac.Id,
                Date = new DateTime(2026, 3, 5),
                Km = 130000,
                Liters = 45m,
                TotalCost = 2000m
            });

            Assert.True(result.Success);
            Assert.Equal(130000, _db.AraciYenidenOku(arac.Id).CurrentKm);
        }

        [Fact]
        public async Task FuelAdd_EsitKilometreAracaYazilmaz()
        {
            var userId = _db.KullaniciEkle("a@garajim.local").Id;
            var arac = _db.AracEkle(userId, "34AAA111", 100000);
            var manager = new FuelManager(_db.FuelDal, _db.VehicleDal);

            await manager.AddAsync(userId, new FuelCreateDto
            {
                VehicleId = arac.Id,
                Date = new DateTime(2026, 3, 5),
                Km = 100000,
                Liters = 45m,
                TotalCost = 2000m
            });

            Assert.Equal(100000, _db.AraciYenidenOku(arac.Id).CurrentKm);
        }

        [Fact]
        public async Task ArdisikKayitlarEnYuksekKilometreyiKorur()
        {
            var userId = _db.KullaniciEkle("a@garajim.local").Id;
            var arac = _db.AracEkle(userId, "34AAA111", 100000);
            var fuelManager = new FuelManager(_db.FuelDal, _db.VehicleDal);
            var maintenanceManager = new MaintenanceManager(_db.MaintenanceDal, _db.VehicleDal);

            await fuelManager.AddAsync(userId, new FuelCreateDto
            {
                VehicleId = arac.Id,
                Date = new DateTime(2026, 3, 1),
                Km = 115000,
                Liters = 40m,
                TotalCost = 1800m
            });
            await maintenanceManager.AddAsync(userId, new MaintenanceCreateDto
            {
                VehicleId = arac.Id,
                Type = MaintenanceType.FrenBakimi,
                Date = new DateTime(2026, 3, 10),
                Km = 108000,
                Cost = 3000m
            });
            await fuelManager.AddAsync(userId, new FuelCreateDto
            {
                VehicleId = arac.Id,
                Date = new DateTime(2026, 3, 20),
                Km = 118000,
                Liters = 42m,
                TotalCost = 1900m
            });

            Assert.Equal(118000, _db.AraciYenidenOku(arac.Id).CurrentKm);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
