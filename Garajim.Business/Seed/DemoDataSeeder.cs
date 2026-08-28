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

        public DemoDataSeeder(
            ICompanyDal companyDal,
            IUserDal userDal,
            IVehicleDal vehicleDal,
            IMaintenanceDal maintenanceDal,
            IFuelDal fuelDal,
            IExpenseDal expenseDal,
            IReminderDal reminderDal,
            IVehicleAssignmentDal assignmentDal)
        {
            _companyDal = companyDal;
            _userDal = userDal;
            _vehicleDal = vehicleDal;
            _maintenanceDal = maintenanceDal;
            _fuelDal = fuelDal;
            _expenseDal = expenseDal;
            _reminderDal = reminderDal;
            _assignmentDal = assignmentDal;
        }

        public async Task<bool> RunAsync()
        {
            if (await _userDal.ExistsForRegistrationAsync(DemoEmail))
            {
                return false;
            }

            var bugun = DateTime.UtcNow.Date;

            var company = new Company
            {
                Name = DemoCompanyName,
                PlanType = PlanType.Standart,
                CreatedAt = DateTime.UtcNow
            };
            await _companyDal.AddAsync(company);

            HashingHelper.CreatePasswordHash(DemoPassword, out var passwordHash, out var passwordSalt);
            var demoUser = new AppUser
            {
                CompanyId = company.Id,
                Role = CompanyRole.Owner,
                IsActive = true,
                Email = DemoEmail,
                FullName = "Demo Kullanıcı",
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                CreatedAt = DateTime.UtcNow
            };
            await _userDal.AddAsync(demoUser);

            HashingHelper.CreatePasswordHash(DemoDriverPassword, out var driverHash, out var driverSalt);
            var demoDriver = new AppUser
            {
                CompanyId = company.Id,
                Role = CompanyRole.Driver,
                IsActive = true,
                Email = DemoDriverEmail,
                FullName = "Demo Sürücü",
                PasswordHash = driverHash,
                PasswordSalt = driverSalt,
                CreatedAt = DateTime.UtcNow
            };
            await _userDal.AddAsync(demoDriver);

            var vehicle = new Vehicle
            {
                CompanyId = company.Id,
                UserId = demoUser.Id,
                Plate = DemoPlate,
                Brand = "Renault",
                Model = "Clio",
                Year = 2018,
                CurrentKm = 122000,
                FuelType = FuelType.Benzin,
                CreatedAt = DateTime.UtcNow
            };
            await _vehicleDal.AddAsync(vehicle);

            await _assignmentDal.AddAsync(new VehicleAssignment
            {
                CompanyId = company.Id,
                VehicleId = vehicle.Id,
                UserId = demoDriver.Id,
                StartDate = bugun.AddDays(-60),
                EndDate = null,
                AssignedByUserId = demoUser.Id,
                CreatedAt = DateTime.UtcNow
            });

            await _maintenanceDal.AddAsync(new MaintenanceRecord
            {
                CompanyId = company.Id,
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
                CompanyId = company.Id,
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
                CompanyId = company.Id,
                VehicleId = vehicle.Id,
                Date = bugun.AddDays(-38),
                Km = 120900,
                Liters = 42m,
                TotalCost = 1980m
            });
            await _fuelDal.AddAsync(new FuelRecord
            {
                CompanyId = company.Id,
                VehicleId = vehicle.Id,
                Date = bugun.AddDays(-20),
                Km = 121500,
                Liters = 38m,
                TotalCost = 1810m
            });
            await _fuelDal.AddAsync(new FuelRecord
            {
                CompanyId = company.Id,
                VehicleId = vehicle.Id,
                Date = bugun.AddDays(-5),
                Km = 122000,
                Liters = 40m,
                TotalCost = 1920m
            });

            await _expenseDal.AddAsync(new ExpenseRecord
            {
                CompanyId = company.Id,
                VehicleId = vehicle.Id,
                Category = ExpenseCategory.Kasko,
                Date = bugun.AddDays(-90),
                Amount = 14500m,
                Note = "Yıllık kasko poliçesi"
            });
            await _expenseDal.AddAsync(new ExpenseRecord
            {
                CompanyId = company.Id,
                VehicleId = vehicle.Id,
                Category = ExpenseCategory.Otopark,
                Date = bugun.AddDays(-12),
                Amount = 350m,
                Note = "Aylık otopark"
            });

            await _reminderDal.AddAsync(new Reminder
            {
                CompanyId = company.Id,
                VehicleId = vehicle.Id,
                Type = ReminderType.Muayene,
                DueDate = bugun.AddDays(21),
                Note = "Araç muayenesi yaklaşıyor",
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            });

            return true;
        }
    }
}
