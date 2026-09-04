using Garajim.Business.Abstract;
using Moq;
using Garajim.Core.Multitenancy;
using Garajim.Business.Concrete;
using Garajim.Business.Seed;
using Garajim.Business.Concrete.Evraklar;
using Garajim.Business.Concrete.Planlar;
using Microsoft.Extensions.Configuration;
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
            KmLogDal = new EfKmDuzeltmeLogDal(Context);
            PlanKurallari = new PlanKurallari(new ConfigurationBuilder().Build());
            UserDal = new EfUserDal(Context);
            VehicleDal = new EfVehicleDal(Context);
            MaintenanceDal = new EfMaintenanceDal(Context);
            DocumentDal = new EfDocumentDal(Context);
            DocumentService = new Mock<IDocumentService>().Object;
            FuelDal = new EfFuelDal(Context);
            ExpenseDal = new EfExpenseDal(Context);
            ReminderDal = new EfReminderDal(Context);
            AssignmentDal = new EfVehicleAssignmentDal(Context);
            PartDal = new EfMaintenancePartDal(Context);
            EvrakDal = new EfEvrakDal(Context);
            ReceiptDraftDal = new EfReceiptDraftDal(Context);
            LastikDal = new EfLastikDal(Context);
            HasarDosyasiDal = new EfHasarDosyasiDal(Context);
            HasarFotoDal = new EfHasarFotoDal(Context);
            AracDegerDal = new EfAracDegerDal(Context);
            YolculukDal = new EfYolculukDal(Context);
            UstaSohbetDal = new EfUstaSohbetDal(Context);
            UstaMesajDal = new EfUstaMesajDal(Context);
            UstaOnayDal = new EfUstaOnayDal(Context);
            UstaCozumOzetiDal = new EfUstaCozumOzetiDal(Context);
            EvrakKurallari = new EvrakKurallari(new ConfigurationBuilder().Build());
            UnitOfWork = new EfUnitOfWork(Context);
            VehicleAccess = new VehicleAccessManager(UserDal, VehicleDal, AssignmentDal);
            DegerService = new DegerManager(AracDegerDal, VehicleAccess, new SahteDegerTahminEdici());
        }

        public GarajimDbContext Context { get; }

        public TenantContext Tenant { get; }

        public Company VarsayilanSirket { get; }

        public int CompanyId => VarsayilanSirket.Id;

        public EfCompanyDal CompanyDal { get; }

        public EfKmDuzeltmeLogDal KmLogDal { get; }

        public PlanKurallari PlanKurallari { get; }

        public EfUserDal UserDal { get; }

        public EfVehicleDal VehicleDal { get; }

        public EfMaintenanceDal MaintenanceDal { get; }

        public EfDocumentDal DocumentDal { get; }

        public IDocumentService DocumentService { get; }

        public EfFuelDal FuelDal { get; }

        public EfExpenseDal ExpenseDal { get; }

        public EfReminderDal ReminderDal { get; }

        public EfVehicleAssignmentDal AssignmentDal { get; }

        public EfMaintenancePartDal PartDal { get; }

        public EfEvrakDal EvrakDal { get; }

        public EfReceiptDraftDal ReceiptDraftDal { get; }

        public EfLastikDal LastikDal { get; }
        public EfHasarDosyasiDal HasarDosyasiDal { get; }
        public EfHasarFotoDal HasarFotoDal { get; }
        public EfAracDegerDal AracDegerDal { get; }
        public DegerManager DegerService { get; }

        public EfYolculukDal YolculukDal { get; }

        public EfUstaSohbetDal UstaSohbetDal { get; }

        public EfUstaMesajDal UstaMesajDal { get; }

        public EfUstaOnayDal UstaOnayDal { get; }

        public EfUstaCozumOzetiDal UstaCozumOzetiDal { get; }

        public EvrakKurallari EvrakKurallari { get; }

        public EfUnitOfWork UnitOfWork { get; }

        public VehicleAccessManager VehicleAccess { get; }

        public DemoDataSeeder DemoSeeder()
        {
            return new DemoDataSeeder(CompanyDal, UserDal, VehicleDal, MaintenanceDal, FuelDal, ExpenseDal, ReminderDal,
                AssignmentDal, EvrakDal, LastikDal, PartDal, HasarDosyasiDal, AracDegerDal, YolculukDal, Tenant);
        }

        public Company SirketEkle(string ad)
        {
            var company = new Company
            {
                Name = ad,
                PlanType = PlanType.Bireysel,
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

        public AppUser KullaniciEkle(string email, CompanyRole role)
        {
            return KullaniciEkle(email, CompanyId, role);
        }

        public AppUser KullaniciEkle(string email, int companyId)
        {
            return KullaniciEkle(email, companyId, CompanyRole.Owner);
        }

        public AppUser KullaniciEkle(string email, int companyId, CompanyRole role)
        {
            var user = new AppUser
            {
                CompanyId = companyId,
                Role = role,
                IsActive = true,
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
            var companyId = Context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.CompanyId)
                .Single();

            return AracEkleSirketle(userId, plaka, companyId, currentKm);
        }

        public Vehicle AracEkleSirketle(int userId, string plaka, int companyId, int currentKm = 100000)
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
