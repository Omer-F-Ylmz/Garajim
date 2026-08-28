using Garajim.Core.Multitenancy;
using Garajim.Dal.Concrete;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public sealed class SqliteTestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SqliteTestDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<GarajimDbContext>()
                .UseSqlite(_connection)
                .EnableSensitiveDataLogging()
                .Options;

            Tenant = new TenantContext();
            Context = new SqliteGarajimDbContext(options, Tenant);
            Context.Database.EnsureCreated();

            VarsayilanSirket = SirketEkle("Varsayılan Şirket");
            Tenant.SetCompany(VarsayilanSirket.Id);

            CompanyDal = new EfCompanyDal(Context);
            UserDal = new EfUserDal(Context);
            VehicleDal = new EfVehicleDal(Context);
            MaintenanceDal = new EfMaintenanceDal(Context);
            FuelDal = new EfFuelDal(Context);
            ExpenseDal = new EfExpenseDal(Context);
            ReminderDal = new EfReminderDal(Context);
        }

        public GarajimDbContext Context { get; }

        public TenantContext Tenant { get; }

        public Company VarsayilanSirket { get; }

        public int CompanyId => VarsayilanSirket.Id;

        public EfCompanyDal CompanyDal { get; }

        public EfUserDal UserDal { get; }

        public EfVehicleDal VehicleDal { get; }

        public EfMaintenanceDal MaintenanceDal { get; }

        public EfFuelDal FuelDal { get; }

        public EfExpenseDal ExpenseDal { get; }

        public EfReminderDal ReminderDal { get; }

        public Company SirketEkle(string ad)
        {
            var company = new Company
            {
                Name = ad,
                PlanType = PlanType.Standart,
                CreatedAt = new DateTime(2026, 1, 1)
            };

            Context.Companies.Add(company);
            Context.SaveChanges();
            return company;
        }

        public AppUser KullaniciEkle(string email)
        {
            return KullaniciEkle(email, CompanyId);
        }

        public AppUser KullaniciEkle(string email, int companyId)
        {
            var user = new AppUser
            {
                CompanyId = companyId,
                Email = email,
                FullName = email,
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                CreatedAt = new DateTime(2026, 1, 1)
            };

            Context.Users.Add(user);
            Context.SaveChanges();
            return user;
        }

        public Vehicle AracEkle(int userId, string plaka, int currentKm = 100000)
        {
            var companyId = Context.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.CompanyId).Single();
            return AracEkle(userId, plaka, companyId, currentKm);
        }

        public Vehicle AracEkle(int userId, string plaka, int companyId, int currentKm = 100000)
        {
            var vehicle = new Vehicle
            {
                CompanyId = companyId,
                UserId = userId,
                Plate = plaka,
                Brand = "Renault",
                Model = "Clio",
                Year = 2018,
                CurrentKm = currentKm,
                FuelType = FuelType.Benzin,
                CreatedAt = new DateTime(2026, 1, 1)
            };

            Context.Vehicles.Add(vehicle);
            Context.SaveChanges();
            return vehicle;
        }

        public Vehicle AraciYenidenOku(int vehicleId)
        {
            return Context.Vehicles.AsNoTracking().Single(v => v.Id == vehicleId);
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
