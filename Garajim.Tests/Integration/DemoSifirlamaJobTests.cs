using Garajim.Business.Jobs;
using Microsoft.EntityFrameworkCore;
using Garajim.Business.Seed;
using Garajim.Core.Multitenancy;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class DemoSifirlamaJobTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        public void Dispose() => _db.Dispose();

        private DemoSifirlamaJob Job(bool acik)
        {
            var yapilandirma = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["DemoSeed:Enabled"] = acik ? "true" : "false"
                })
                .Build();

            return new DemoSifirlamaJob(
                _db.CompanyDal, _db.UserDal, _db.DocumentDal, new SahteBelgeServisi(),
                _db.DemoSeeder(), yapilandirma, _db.Tenant);
        }

        private int DemoAracSayisi()
        {
            var demo = _db.Context.Users.IgnoreQueryFilters().Single(u => u.Email == DemoDataSeeder.DemoEmail);
            return _db.Context.Vehicles.IgnoreQueryFilters().Count(v => v.CompanyId == demo.CompanyId);
        }

        [Fact]
        public async Task KapaliyknCalismaz()
        {
            await Job(true).RunAsync();
            var once = DemoAracSayisi();

            _db.Context.ChangeTracker.Clear();
            await Job(false).RunAsync();

            Assert.Equal(once, DemoAracSayisi());
        }

        [Fact]
        public async Task SifirlamaSonrasiSayilarSabitKalir()
        {
            await Job(true).RunAsync();
            var ilk = DemoAracSayisi();

            _db.Context.ChangeTracker.Clear();
            await Job(true).RunAsync();

            Assert.Equal(ilk, DemoAracSayisi());
            Assert.True(ilk > 0);
        }

        [Fact]
        public async Task DemoKullanicisiVeSifresiSabitKalir()
        {
            await Job(true).RunAsync();
            _db.Context.ChangeTracker.Clear();
            await Job(true).RunAsync();

            var demo = _db.Context.Users.IgnoreQueryFilters().Single(u => u.Email == DemoDataSeeder.DemoEmail);

            Assert.True(demo.IsActive);
            Assert.True(Core.Utilities.Security.HashingHelper.VerifyPasswordHash(
                DemoDataSeeder.DemoPassword, demo.PasswordHash, demo.PasswordSalt));
        }

        [Fact]
        public async Task YabanciSirketinVerisineDokunulmaz()
        {
            await Job(true).RunAsync();
            _db.Context.ChangeTracker.Clear();

            var komsu = new Company { Name = "Komşu", PlanType = PlanType.Bireysel, CreatedAt = DateTime.UtcNow };
            _db.Context.Companies.Add(komsu);
            _db.Context.SaveChanges();

            using (var kapsam = SystemScope.For(_db.Tenant, komsu.Id))
            {
                _db.Context.Vehicles.Add(new Vehicle
                {
                    CompanyId = komsu.Id,
                    UserId = _db.Context.Users.IgnoreQueryFilters().First().Id,
                    Plate = TestPlaka.Uret(),
                    Brand = "Komsu", Model = "Arac", Year = 2020, CurrentKm = 1000,
                    FuelType = FuelType.Benzin, CreatedAt = DateTime.UtcNow
                });
                _db.Context.SaveChanges();
            }

            _db.Context.ChangeTracker.Clear();
            await Job(true).RunAsync();

            Assert.Equal(1, _db.Context.Vehicles.IgnoreQueryFilters().Count(v => v.CompanyId == komsu.Id));
        }
    }
}
