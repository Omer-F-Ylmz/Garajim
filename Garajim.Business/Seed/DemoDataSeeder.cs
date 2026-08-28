using Garajim.Core.Multitenancy;
using Garajim.Core.Utilities.Security;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Business.Seed
{
    public class DemoDataSeeder
    {
        public const string DemoEmail = "demo@garajim.app";
        public const string DemoPassword = "Demo1234!";
        public const string DemoDriverEmail = "surucu@garajim.app";
        public const string DemoDriverPassword = "Surucu1234!";
        public const string DemoPlate = "34DEMO34";
        public const string DemoCompanyName = "Garajım Demo";

        private readonly ICompanyDal _companyDal;
        private readonly IUserDal _userDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IFuelDal _fuelDal;
        private readonly IExpenseDal _expenseDal;
        private readonly IReminderDal _reminderDal;
        private readonly IVehicleAssignmentDal _assignmentDal;
        private readonly TenantContext _tenantContext;

        public DemoDataSeeder(
            ICompanyDal companyDal,
            IUserDal userDal,
            IVehicleDal vehicleDal,
            IMaintenanceDal maintenanceDal,
            IFuelDal fuelDal,
            IExpenseDal expenseDal,
            IReminderDal reminderDal,
            IVehicleAssignmentDal assignmentDal,
            TenantContext tenantContext)
        {
            _companyDal = companyDal;
            _userDal = userDal;
            _vehicleDal = vehicleDal;
            _maintenanceDal = maintenanceDal;
            _fuelDal = fuelDal;
            _expenseDal = expenseDal;
            _reminderDal = reminderDal;
            _assignmentDal = assignmentDal;
            _tenantContext = tenantContext;
        }

        public async Task<bool> RunAsync()
        {
            var eklendi = false;

            var owner = await _userDal.GetForAuthenticationAsync(DemoEmail);

            var company = owner != null
                ? await _companyDal.GetAsync(c => c.Id == owner.CompanyId)
                : await _companyDal.GetAsync(c => c.Name == DemoCompanyName);

            if (company == null)
            {
                company = new Company
                {
                    Name = DemoCompanyName,
                    PlanType = PlanType.Standart,
                    CreatedAt = DateTime.UtcNow
                };
                await _companyDal.AddAsync(company);
                eklendi = true;
            }

            _tenantContext.SetCompany(company.Id);
            try
            {
                if (owner == null)
                {
                    owner = KullaniciOlustur(company.Id, DemoEmail, DemoPassword, "Demo Kullanıcı", CompanyRole.Owner);
                    await _userDal.AddAsync(owner);
                    eklendi = true;
                }

                var driver = await _userDal.GetAsync(u => u.Email == DemoDriverEmail);
                if (driver == null && !await _userDal.ExistsForRegistrationAsync(DemoDriverEmail))
                {
                    driver = KullaniciOlustur(company.Id, DemoDriverEmail, DemoDriverPassword, "Demo Sürücü", CompanyRole.Driver);
                    await _userDal.AddAsync(driver);
                    eklendi = true;
                }

                var vehicle = await _vehicleDal.GetAsync(v => v.Plate == DemoPlate);
                if (vehicle == null)
                {
                    vehicle = await AracVeKayitlariniEkleAsync(company.Id, owner.Id);
                    eklendi = true;
                }

                if (driver != null)
                {
                    var aktif = await _assignmentDal.GetActiveByVehicleAsync(vehicle.Id);
                    if (aktif == null)
                    {
                        await _assignmentDal.AddAsync(new VehicleAssignment
                        {
                            CompanyId = company.Id,
                            VehicleId = vehicle.Id,
                            UserId = driver.Id,
                            StartDate = DateTime.UtcNow,
                            EndDate = null,
                            AssignedByUserId = owner.Id,
                            CreatedAt = DateTime.UtcNow
                        });
                        eklendi = true;
                    }
                }
            }
            finally
            {
                _tenantContext.Clear();
            }

            return eklendi;
        }

        private static AppUser KullaniciOlustur(
            int companyId, string email, string password, string fullName, CompanyRole role)
        {
            HashingHelper.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);
            return new AppUser
            {
                CompanyId = companyId,
                Role = role,
                IsActive = true,
                Email = email,
                FullName = fullName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                CreatedAt = DateTime.UtcNow
            };
        }

        private async Task<Vehicle> AracVeKayitlariniEkleAsync(int companyId, int ownerId)
        {
            var bugun = DateTime.UtcNow.Date;

            var vehicle = new Vehicle
            {
                CompanyId = companyId,
                UserId = ownerId,
                Plate = DemoPlate,
                Brand = "Renault",
                Model = "Clio",
                Year = 2018,
                CurrentKm = 122000,
                FuelType = FuelType.Benzin,
                CreatedAt = DateTime.UtcNow
            };
            await _vehicleDal.AddAsync(vehicle);

            await _maintenanceDal.AddAsync(new MaintenanceRecord
            {
                CompanyId = companyId,
                VehicleId = vehicle.Id,
                Type = MaintenanceType.PeriyodikBakim,
                Date = bugun.AddDays(-120),
                Km = 118000,
                Cost = 4850m,
                ServiceName = "Yetkili Servis",
                Note = "40.000 km bakımı"
            });
            await _maintenanceDal.AddAsync(new MaintenanceRecord
            {
                CompanyId = companyId,
                VehicleId = vehicle.Id,
                Type = MaintenanceType.LastikDegisimi,
                Date = bugun.AddDays(-45),
                Km = 120500,
                Cost = 9200m,
                ServiceName = "Lastikçi",
                Note = "Dört mevsim lastik"
            });

            await _fuelDal.AddAsync(new FuelRecord
            {
                CompanyId = companyId,
                VehicleId = vehicle.Id,
                Date = bugun.AddDays(-38),
                Km = 120900,
                Liters = 42m,
                TotalCost = 1980m
            });
            await _fuelDal.AddAsync(new FuelRecord
            {
                CompanyId = companyId,
                VehicleId = vehicle.Id,
                Date = bugun.AddDays(-20),
                Km = 121500,
                Liters = 38m,
                TotalCost = 1810m
            });
            await _fuelDal.AddAsync(new FuelRecord
            {
                CompanyId = companyId,
                VehicleId = vehicle.Id,
                Date = bugun.AddDays(-5),
                Km = 122000,
                Liters = 40m,
                TotalCost = 1920m
            });

            await _expenseDal.AddAsync(new ExpenseRecord
            {
                CompanyId = companyId,
                VehicleId = vehicle.Id,
                Category = ExpenseCategory.Kasko,
                Date = bugun.AddDays(-90),
                Amount = 14500m,
                Note = "Yıllık kasko poliçesi"
            });
            await _expenseDal.AddAsync(new ExpenseRecord
            {
                CompanyId = companyId,
                VehicleId = vehicle.Id,
                Category = ExpenseCategory.Otopark,
                Date = bugun.AddDays(-12),
                Amount = 350m,
                Note = "Aylık otopark"
            });

            await _reminderDal.AddAsync(new Reminder
            {
                CompanyId = companyId,
                VehicleId = vehicle.Id,
                Type = ReminderType.Muayene,
                DueDate = bugun.AddDays(21),
                Note = "Araç muayenesi yaklaşıyor",
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            });

            return vehicle;
        }
    }
}
