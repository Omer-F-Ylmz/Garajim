using Garajim.Business.Jobs;
using Garajim.Core.Multitenancy;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class UstaOzetKayipTests : IDisposable
    {
        private sealed class PatlayanOzetDal : IUstaCozumOzetiDal
        {
            private readonly IUstaCozumOzetiDal _ic;

            public PatlayanOzetDal(IUstaCozumOzetiDal ic)
            {
                _ic = ic;
            }

            public bool Patlasin { get; set; } = true;

            public Task<UstaCozumOzeti> BulAsync(string marka, string model, string motor, string kategori, string parca)
                => _ic.BulAsync(marka, model, motor, kategori, parca);

            public Task<List<UstaCozumOzeti>> GetTumuAsync() => _ic.GetTumuAsync();

            public Task AddAsync(UstaCozumOzeti entity)
                => Patlasin ? throw new InvalidOperationException("özet yazılamadı") : _ic.AddAsync(entity);

            public Task UpdateAsync(UstaCozumOzeti entity)
                => Patlasin ? throw new InvalidOperationException("özet yazılamadı") : _ic.UpdateAsync(entity);

            public Task DeleteAsync(UstaCozumOzeti entity) => _ic.DeleteAsync(entity);
            public Task<UstaCozumOzeti> GetAsync(System.Linq.Expressions.Expression<Func<UstaCozumOzeti, bool>> filter) => _ic.GetAsync(filter);
            public Task<List<UstaCozumOzeti>> GetListAsync(System.Linq.Expressions.Expression<Func<UstaCozumOzeti, bool>> filter = null) => _ic.GetListAsync(filter);
            public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<UstaCozumOzeti, bool>> filter = null) => _ic.CountAsync(filter);
            public Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<UstaCozumOzeti, bool>> filter) => _ic.AnyAsync(filter);
        }

        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        public void Dispose()
        {
            _db.Dispose();
        }

        private (AppUser Sahip, Vehicle Arac, UstaMesaj Mesaj) VeriKur()
        {
            var sahip = new AppUser
            {
                CompanyId = _db.CompanyId,
                Role = CompanyRole.Owner,
                IsActive = true,
                EmailDogrulandi = true,
                Email = "ozet-sahip@garajim.local",
                FullName = "Özet Sahibi",
                PasswordHash = new byte[] { 1 },
                PasswordSalt = new byte[] { 1 },
                CreatedAt = DateTime.UtcNow
            };
            _db.Context.Users.Add(sahip);
            _db.Context.SaveChanges();

            var arac = new Vehicle
            {
                CompanyId = _db.CompanyId,
                UserId = sahip.Id,
                Plate = "34OZT001",
                Brand = "Renault",
                Model = "Clio",
                Motor = "1.5 dCi",
                Year = 2019,
                CurrentKm = 100000,
                FuelType = FuelType.Dizel,
                CreatedAt = DateTime.UtcNow
            };
            _db.Context.Vehicles.Add(arac);
            _db.Context.SaveChanges();

            var bakim = new MaintenanceRecord
            {
                CompanyId = _db.CompanyId,
                VehicleId = arac.Id,
                Type = MaintenanceType.FrenBakimi,
                Date = DateTime.UtcNow.Date.AddDays(-5),
                Km = 100000,
                Cost = 2500m,
                ServiceName = "Servis"
            };
            _db.Context.MaintenanceRecords.Add(bakim);
            _db.Context.SaveChanges();

            _db.Context.MaintenanceParts.Add(new MaintenancePart
            {
                CompanyId = _db.CompanyId,
                MaintenanceRecordId = bakim.Id,
                VehicleId = arac.Id,
                ParcaTuru = ParcaTuru.FrenBalatasiOn,
                Adet = 1
            });

            var sohbet = new UstaSohbet
            {
                CompanyId = _db.CompanyId,
                VehicleId = arac.Id,
                UserId = sahip.Id,
                Baslik = "Fren sesi",
                OlusturmaTarihi = DateTime.UtcNow
            };
            _db.Context.UstaSohbetleri.Add(sohbet);
            _db.Context.SaveChanges();

            var mesaj = new UstaMesaj
            {
                CompanyId = _db.CompanyId,
                SohbetId = sohbet.Id,
                Rol = UstaRol.Usta,
                Metin = "Fren balatası olabilir.",
                BilgiKategorisi = "Fren",
                GeriBildirim = UstaGeriBildirim.Olumlu,
                CozumBakimId = bakim.Id,
                Ozetlendi = false,
                OlusturmaTarihi = DateTime.UtcNow
            };
            _db.Context.UstaMesajlari.Add(mesaj);
            _db.Context.SaveChanges();

            return (sahip, arac, mesaj);
        }

        private UstaOzetJob Job(IUstaCozumOzetiDal ozetDal)
        {
            return new UstaOzetJob(
                _db.CompanyDal, _db.UstaMesajDal, _db.UstaSohbetDal, _db.VehicleDal,
                _db.PartDal, ozetDal, _db.UserDal, _db.UnitOfWork, _db.Tenant);
        }

        [Fact]
        public async Task OzetYazilamazsaMesajOzetlendiIsaretlenmez()
        {
            var veri = VeriKur();
            var patlayan = new PatlayanOzetDal(_db.UstaCozumOzetiDal);

            await Job(patlayan).RunAsync();

            var mesaj = await _db.Context.UstaMesajlari.IgnoreQueryFilters()
                .AsNoTracking().SingleAsync(m => m.Id == veri.Mesaj.Id);

            Assert.False(mesaj.Ozetlendi,
                "Özet yazılamadığı hâlde mesaj özetlendi işaretlendi; sayım kalıcı olarak kaybolur.");
        }

        [Fact]
        public async Task HatadanSonraYenidenCalisincaSayimKaybolmaz()
        {
            var veri = VeriKur();
            var patlayan = new PatlayanOzetDal(_db.UstaCozumOzetiDal);

            await Job(patlayan).RunAsync();

            patlayan.Patlasin = false;
            await Job(patlayan).RunAsync();

            var ozetler = await _db.Context.UstaCozumOzetleri.IgnoreQueryFilters().AsNoTracking().ToListAsync();
            var mesaj = await _db.Context.UstaMesajlari.IgnoreQueryFilters()
                .AsNoTracking().SingleAsync(m => m.Id == veri.Mesaj.Id);

            Assert.Single(ozetler);
            Assert.Equal(1, ozetler[0].Sayi);
            Assert.True(mesaj.Ozetlendi);
        }

        [Fact]
        public async Task BasariliCalismadaSayimYazilirVeMesajIsaretlenir()
        {
            var veri = VeriKur();

            await Job(_db.UstaCozumOzetiDal).RunAsync();

            var ozetler = await _db.Context.UstaCozumOzetleri.IgnoreQueryFilters().AsNoTracking().ToListAsync();
            var mesaj = await _db.Context.UstaMesajlari.IgnoreQueryFilters()
                .AsNoTracking().SingleAsync(m => m.Id == veri.Mesaj.Id);

            Assert.Single(ozetler);
            Assert.Equal("Fren", ozetler[0].BelirtiKategori);
            Assert.Equal(1, ozetler[0].Sayi);
            Assert.True(mesaj.Ozetlendi);
        }

        [Fact]
        public async Task IkinciCalismaSayimiCiftlemez()
        {
            VeriKur();

            await Job(_db.UstaCozumOzetiDal).RunAsync();
            await Job(_db.UstaCozumOzetiDal).RunAsync();

            var ozetler = await _db.Context.UstaCozumOzetleri.IgnoreQueryFilters().AsNoTracking().ToListAsync();

            Assert.Single(ozetler);
            Assert.Equal(1, ozetler[0].Sayi);
        }
    }
}
