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
        private readonly IEvrakDal _evrakDal;
        private readonly ILastikDal _lastikDal;
        private readonly IMaintenancePartDal _partDal;
        private readonly IHasarDosyasiDal _hasarDal;
        private readonly IAracDegerDal _degerDal;
        private readonly IYolculukDal _yolculukDal;
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
            IEvrakDal evrakDal,
            ILastikDal lastikDal,
            IMaintenancePartDal partDal,
            IHasarDosyasiDal hasarDal,
            IAracDegerDal degerDal,
            IYolculukDal yolculukDal,
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
            _evrakDal = evrakDal;
            _lastikDal = lastikDal;
            _partDal = partDal;
            _hasarDal = hasarDal;
            _degerDal = degerDal;
            _yolculukDal = yolculukDal;
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
                    PlanType = PlanType.Bireysel,
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

                eklendi |= await ZenginlestirAsync(company.Id, owner.Id, vehicle);

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

        private async Task<bool> ZenginlestirAsync(int companyId, int ownerId, Vehicle vehicle)
        {
            var bugun = DateTime.UtcNow.Date;
            var eklendi = false;

            if (vehicle.KasaTipi == null || string.IsNullOrWhiteSpace(vehicle.Vites) || string.IsNullOrWhiteSpace(vehicle.Motor))
            {
                vehicle.KasaTipi = vehicle.KasaTipi ?? KasaTipi.Hatchback5;
                vehicle.Vites = string.IsNullOrWhiteSpace(vehicle.Vites) ? "Düz" : vehicle.Vites;
                vehicle.Motor = string.IsNullOrWhiteSpace(vehicle.Motor) ? "1.5 dCi" : vehicle.Motor;
                await _vehicleDal.UpdateAsync(vehicle);
                eklendi = true;
            }

            if (!await _evrakDal.AnyAsync(e => e.VehicleId == vehicle.Id && e.EvrakTuru == EvrakTuru.Muayene))
            {
                await _evrakDal.AddAsync(new EvrakKaydi
                {
                    CompanyId = companyId,
                    VehicleId = vehicle.Id,
                    EvrakTuru = EvrakTuru.Muayene,
                    BaslangicTarihi = bugun.AddYears(-2).AddDays(18),
                    BitisTarihi = bugun.AddDays(18),
                    Saglayici = "TÜVTÜRK",
                    Not = "Demo kaydı",
                    Aktif = true,
                    OlusturmaTarihi = DateTime.UtcNow
                });
                eklendi = true;
            }

            if (!await _evrakDal.AnyAsync(e => e.VehicleId == vehicle.Id && e.EvrakTuru == EvrakTuru.TrafikSigortasi))
            {
                await _evrakDal.AddAsync(new EvrakKaydi
                {
                    CompanyId = companyId,
                    VehicleId = vehicle.Id,
                    EvrakTuru = EvrakTuru.TrafikSigortasi,
                    BaslangicTarihi = bugun.AddDays(-160),
                    BitisTarihi = bugun.AddDays(205),
                    Saglayici = "Demo Sigorta",
                    PoliceNo = "DMO-2026-0041",
                    Aktif = true,
                    OlusturmaTarihi = DateTime.UtcNow
                });
                eklendi = true;
            }

            if (!await _lastikDal.AnyAsync(l => l.VehicleId == vehicle.Id))
            {
                await _lastikDal.AddAsync(new LastikSeti
                {
                    CompanyId = companyId,
                    VehicleId = vehicle.Id,
                    Ad = "Kış seti 2025",
                    Mevsim = LastikMevsimi.Kis,
                    Marka = "Michelin",
                    Ebat = "195/65 R15",
                    DisDerinligiMm = 6.5m,
                    TakilmaTarihi = bugun.AddDays(-300),
                    TakilmaKm = 114000,
                    SokulmeTarihi = bugun.AddDays(-160),
                    SokulmeKm = 118600,
                    ToplamKm = 4600,
                    Takili = false,
                    OlusturmaTarihi = DateTime.UtcNow
                });

                await _lastikDal.AddAsync(new LastikSeti
                {
                    CompanyId = companyId,
                    VehicleId = vehicle.Id,
                    Ad = "Yaz seti 2026",
                    Mevsim = LastikMevsimi.Yaz,
                    Marka = "Goodyear",
                    Ebat = "195/65 R15",
                    DisDerinligiMm = 7.2m,
                    TakilmaTarihi = bugun.AddDays(-160),
                    TakilmaKm = 118600,
                    ToplamKm = 0,
                    Takili = true,
                    OlusturmaTarihi = DateTime.UtcNow
                });
                eklendi = true;
            }

            if (!await _partDal.AnyAsync(p => p.VehicleId == vehicle.Id))
            {
                var bakim = new MaintenanceRecord
                {
                    CompanyId = companyId,
                    VehicleId = vehicle.Id,
                    Type = MaintenanceType.YagDegisimi,
                    Date = bugun.AddDays(-60),
                    Km = 119800,
                    Cost = 3750m,
                    ServiceName = "Demo Oto Servis",
                    Note = "Yağ ve filtre bakımı"
                };
                await _maintenanceDal.AddAsync(bakim);

                await _partDal.AddAsync(ParcaOlustur(companyId, vehicle.Id, bakim.Id, ParcaTuru.MotorYagi, "5W-30 tam sentetik", 1, 1450m, "Castrol"));
                await _partDal.AddAsync(ParcaOlustur(companyId, vehicle.Id, bakim.Id, ParcaTuru.YagFiltresi, "Yağ filtresi", 1, 320m, "Mann"));
                await _partDal.AddAsync(ParcaOlustur(companyId, vehicle.Id, bakim.Id, ParcaTuru.PolenFiltresi, "Polen filtresi", 1, 280m, "Bosch"));
                eklendi = true;
            }

            if (!await _hasarDal.AnyAsync(h => h.VehicleId == vehicle.Id))
            {
                await _hasarDal.AddAsync(new HasarDosyasi
                {
                    CompanyId = companyId,
                    VehicleId = vehicle.Id,
                    OlayTarihi = bugun.AddDays(-210),
                    Tur = HasarTuru.Cam,
                    Konum = "Ankara, Eskişehir Yolu",
                    Aciklama = "Ön cama taş çarptı, cam sigorta kapsamında değiştirildi.",
                    OlayKm = 116400,
                    TutanakTuru = TutanakTuru.Yok,
                    SigortaDosyaNo = "DMO-CAM-118",
                    HasarBedeli = 6800m,
                    Durum = HasarDurumu.Kapandi,
                    OlusturanUserId = ownerId,
                    OlusturmaTarihi = DateTime.UtcNow
                });
                eklendi = true;
            }

            if (!await _degerDal.AnyAsync(d => d.VehicleId == vehicle.Id))
            {
                await _degerDal.AddAsync(new AracDeger
                {
                    CompanyId = companyId,
                    VehicleId = vehicle.Id,
                    Tarih = bugun.AddDays(-330),
                    Deger = 780000m,
                    Kaynak = DegerKaynagi.Beyan,
                    Not = "Alım sırasındaki değer",
                    OlusturmaTarihi = DateTime.UtcNow
                });

                await _degerDal.AddAsync(new AracDeger
                {
                    CompanyId = companyId,
                    VehicleId = vehicle.Id,
                    Tarih = bugun.AddDays(-25),
                    Deger = 715000m,
                    Kaynak = DegerKaynagi.Ekspertiz,
                    Not = "Ekspertiz raporu",
                    OlusturmaTarihi = DateTime.UtcNow
                });
                eklendi = true;
            }

            if (!await _yolculukDal.AnyAsync(y => y.VehicleId == vehicle.Id))
            {
                await _yolculukDal.AddAsync(new YolculukKaydi
                {
                    CompanyId = companyId,
                    VehicleId = vehicle.Id,
                    UserId = ownerId,
                    Tarih = bugun.AddDays(-9),
                    BaslangicKm = 121620,
                    BitisKm = 121905,
                    MesafeKm = 285,
                    Amac = YolculukAmaci.Is,
                    Nereden = "Ankara",
                    Nereye = "Eskişehir",
                    Not = "Müşteri ziyareti",
                    OlusturmaTarihi = DateTime.UtcNow
                });
                eklendi = true;
            }

            return eklendi;
        }

        private static MaintenancePart ParcaOlustur(
            int companyId, int vehicleId, int bakimId, ParcaTuru tur, string aciklama, int adet, decimal tutar, string marka)
        {
            return new MaintenancePart
            {
                CompanyId = companyId,
                MaintenanceRecordId = bakimId,
                VehicleId = vehicleId,
                ParcaTuru = tur,
                Aciklama = aciklama,
                Adet = adet,
                Tutar = tutar,
                Marka = marka
            };
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
                EmailDogrulandi = true,
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
