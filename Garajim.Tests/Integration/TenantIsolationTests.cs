using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class TenantIsolationTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private readonly Company _sirketA;
        private readonly Company _sirketB;
        private readonly AppUser _kullaniciA;
        private readonly AppUser _kullaniciB;
        private readonly Vehicle _aracA;
        private readonly Vehicle _aracB;

        public TenantIsolationTests()
        {
            _sirketA = _db.SirketEkle("A Filo");
            _sirketB = _db.SirketEkle("B Filo");

            _kullaniciA = _db.KullaniciEkle("a@garajim.local", _sirketA.Id);
            _kullaniciB = _db.KullaniciEkle("b@garajim.local", _sirketB.Id);

            _aracA = _db.AracEkleSirketle(_kullaniciA.Id, "34AAA111", _sirketA.Id);
            _aracB = _db.AracEkleSirketle(_kullaniciB.Id, "06BBB222", _sirketB.Id);

            KayitEkle(_aracA);
            KayitEkle(_aracB);
        }

        private void KayitEkle(Vehicle arac)
        {
            _db.Context.MaintenanceRecords.Add(new MaintenanceRecord
            {
                CompanyId = arac.CompanyId,
                VehicleId = arac.Id,
                Type = MaintenanceType.PeriyodikBakim,
                Date = new DateTime(2026, 3, 1),
                Km = 100000,
                Cost = 1000m
            });
            _db.Context.FuelRecords.Add(new FuelRecord
            {
                CompanyId = arac.CompanyId,
                VehicleId = arac.Id,
                Date = new DateTime(2026, 3, 2),
                Km = 100100,
                Liters = 40m,
                TotalCost = 1800m
            });
            _db.Context.ExpenseRecords.Add(new ExpenseRecord
            {
                CompanyId = arac.CompanyId,
                VehicleId = arac.Id,
                Category = ExpenseCategory.Kasko,
                Date = new DateTime(2026, 3, 3),
                Amount = 12000m
            });
            _db.Context.Reminders.Add(new Reminder
            {
                CompanyId = arac.CompanyId,
                VehicleId = arac.Id,
                Type = ReminderType.Muayene,
                DueDate = new DateTime(2026, 9, 1),
                CreatedAt = new DateTime(2026, 1, 1)
            });
            _db.Context.SaveChanges();
        }

        [Fact]
        public void AracSorgusu_SahiplikYuklemiOlmadanBileDigerSirketiGizler()
        {
            _db.Tenant.SetCompany(_sirketA.Id);

            var hepsi = _db.Context.Vehicles.AsNoTracking().ToList();

            Assert.Single(hepsi);
            Assert.Equal(_aracA.Id, hepsi[0].Id);
        }

        [Fact]
        public void CocukTablolar_SahiplikYuklemiOlmadanBileDigerSirketiGizler()
        {
            _db.Tenant.SetCompany(_sirketB.Id);

            var bakimlar = _db.Context.MaintenanceRecords.AsNoTracking().ToList();
            var yakitlar = _db.Context.FuelRecords.AsNoTracking().ToList();
            var masraflar = _db.Context.ExpenseRecords.AsNoTracking().ToList();
            var hatirlatmalar = _db.Context.Reminders.AsNoTracking().ToList();

            Assert.All(bakimlar, k => Assert.Equal(_sirketB.Id, k.CompanyId));
            Assert.All(yakitlar, k => Assert.Equal(_sirketB.Id, k.CompanyId));
            Assert.All(masraflar, k => Assert.Equal(_sirketB.Id, k.CompanyId));
            Assert.All(hatirlatmalar, k => Assert.Equal(_sirketB.Id, k.CompanyId));
            Assert.Single(bakimlar);
            Assert.Single(yakitlar);
            Assert.Single(masraflar);
            Assert.Single(hatirlatmalar);
        }

        [Fact]
        public void KullaniciSorgusu_DigerSirketinKullanicisiniGizler()
        {
            _db.Tenant.SetCompany(_sirketA.Id);

            var kullanicilar = _db.Context.Users.AsNoTracking().ToList();

            Assert.Single(kullanicilar);
            Assert.Equal(_kullaniciA.Id, kullanicilar[0].Id);
        }

        [Fact]
        public async Task DalKatmani_FiltresizListelemedeBileSirketDisinaCikmaz()
        {
            _db.Tenant.SetCompany(_sirketA.Id);

            var araclar = await _db.VehicleDal.GetListAsync();
            var bakimlar = await _db.MaintenanceDal.GetListAsync();

            Assert.All(araclar, a => Assert.Equal(_sirketA.Id, a.CompanyId));
            Assert.All(bakimlar, b => Assert.Equal(_sirketA.Id, b.CompanyId));
        }

        [Fact]
        public async Task BaskaSirketinAraciIdIleIstenirseBulunamaz()
        {
            _db.Tenant.SetCompany(_sirketA.Id);
            var manager = new VehicleManager(_db.VehicleDal, _db.UserDal, _db.VehicleAccess, _db.CompanyDal, _db.PlanKurallari, _db.KmLogDal, TestKatalog.Yukle(), _db.DocumentDal, _db.MaintenanceDal, new SahteBelgeServisi(), _db.UnitOfWork);

            var sonuc = await manager.GetByIdAsync(_kullaniciA.Id, _aracB.Id);

            Assert.False(sonuc.Success);
            Assert.Equal(Messages.VehicleNotFound, sonuc.Message);
        }

        [Fact]
        public async Task BaskaSirketinAraciSilinemez()
        {
            _db.Tenant.SetCompany(_sirketA.Id);
            var manager = new VehicleManager(_db.VehicleDal, _db.UserDal, _db.VehicleAccess, _db.CompanyDal, _db.PlanKurallari, _db.KmLogDal, TestKatalog.Yukle(), _db.DocumentDal, _db.MaintenanceDal, new SahteBelgeServisi(), _db.UnitOfWork);

            var sonuc = await manager.DeleteAsync(_kullaniciA.Id, _aracB.Id);

            _db.Tenant.SetCompany(_sirketB.Id);
            var hala = await _db.Context.Vehicles.AsNoTracking().CountAsync(v => v.Id == _aracB.Id);

            Assert.False(sonuc.Success);
            Assert.Equal(1, hala);
        }

        [Fact]
        public void SirketTablosuFiltrelenmez()
        {
            _db.Tenant.SetCompany(_sirketA.Id);

            var sirketler = _db.Context.Companies.AsNoTracking().ToList();

            Assert.True(sirketler.Count >= 2);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
