using Garajim.Business.Concrete;
using Garajim.Dal.Concrete;
using Garajim.Core.Multitenancy;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class GunlukKotaPenceresiTests : IDisposable
    {
        private readonly SqliteConnection _baglanti;
        private readonly GarajimDbContext _context;

        public GunlukKotaPenceresiTests()
        {
            _baglanti = new SqliteConnection("DataSource=:memory:");
            _baglanti.Open();

            var secenekler = new DbContextOptionsBuilder<GarajimDbContext>()
                .UseSqlite(_baglanti)
                .Options;

            var tenant = new TenantContext();
            tenant.SetCompany(1);

            _context = new GarajimDbContext(secenekler, tenant);
            _context.Database.EnsureCreated();

            _context.Companies.Add(new Company { Id = 1, Name = "Kota Testi", PlanType = PlanType.Bireysel, CreatedAt = DateTime.UtcNow });
            _context.Users.Add(new AppUser
            {
                Id = 1,
                CompanyId = 1,
                Email = "kota.local",
                FullName = "Kota",
                PasswordHash = new byte[1],
                PasswordSalt = new byte[1],
                Role = CompanyRole.Owner,
                IsActive = true,
                EmailDogrulandi = true,
                CreatedAt = DateTime.UtcNow
            });
            _context.Vehicles.Add(new Vehicle
            {
                Id = 7,
                CompanyId = 1,
                UserId = 1,
                Plate = "34KOT007",
                Brand = "Fiat",
                Model = "Egea",
                Year = 2019,
                CurrentKm = 1000,
                FuelType = FuelType.Benzin,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
            _baglanti.Dispose();
        }

        private async Task TahminYazAsync(DateTime olusturmaUtc)
        {
            _context.AracDegerleri.Add(new AracDeger
            {
                CompanyId = 1,
                VehicleId = 7,
                Tarih = olusturmaUtc.Date,
                Deger = 500000m,
                Kaynak = DegerKaynagi.Tahmin,
                OlusturmaTarihi = olusturmaUtc
            });

            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task SayacTurkiyeGunuPenceresiniKullanir()
        {
            var gunBasi = new DateTime(2026, 9, 3, 21, 0, 0, DateTimeKind.Utc);

            await TahminYazAsync(gunBasi.AddMinutes(-1));
            await TahminYazAsync(gunBasi);
            await TahminYazAsync(gunBasi.AddHours(8));
            await TahminYazAsync(gunBasi.AddHours(23).AddMinutes(59));
            await TahminYazAsync(gunBasi.AddHours(24));

            var dal = new EfAracDegerDal(_context);

            Assert.Equal(3, await dal.GunlukTahminSayisiAsync(7, gunBasi));
        }

        [Fact]
        public async Task GunBasiUtcTamOlarakYirmiDortSaatlikPencereAcar()
        {
            var gunBasi = Saat.GunBasiUtc();

            await TahminYazAsync(gunBasi);
            await TahminYazAsync(DateTime.UtcNow);

            var dal = new EfAracDegerDal(_context);

            Assert.Equal(2, await dal.GunlukTahminSayisiAsync(7, gunBasi));
        }
    }
}
