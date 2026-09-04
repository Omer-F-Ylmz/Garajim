using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public class OrnekAracManager : IOrnekAracService
    {
        public const string Plaka = "34ORN001";
        public const string Marka = "Fiat";
        public const string Seri = "Egea";
        public const int Yil = 2019;
        public const int BaslangicKm = 78000;
        public const int EvrakKalanGun = 20;

        private readonly IUserDal _userDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IVehicleService _vehicleService;
        private readonly IFuelService _fuelService;
        private readonly IMaintenanceService _maintenanceService;
        private readonly IExpenseService _expenseService;
        private readonly IEvrakService _evrakService;
        private readonly ILastikService _lastikService;
        private readonly ISaat _saat;

        public OrnekAracManager(
            IUserDal userDal,
            IVehicleDal vehicleDal,
            IVehicleService vehicleService,
            IFuelService fuelService,
            IMaintenanceService maintenanceService,
            IExpenseService expenseService,
            IEvrakService evrakService,
            ILastikService lastikService,
            ISaat saat)
        {
            _userDal = userDal;
            _vehicleDal = vehicleDal;
            _vehicleService = vehicleService;
            _fuelService = fuelService;
            _maintenanceService = maintenanceService;
            _expenseService = expenseService;
            _evrakService = evrakService;
            _lastikService = lastikService;
            _saat = saat;
        }

        public async Task<IDataResult<VehicleDto>> OlusturAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<VehicleDto>(Messages.UserNotFound);

            if (await _vehicleDal.AnyAsync(v => v.Ornek))
                return new ErrorDataResult<VehicleDto>(Messages.OrnekAracZatenVar);

            if (await _vehicleDal.AnyAsync(v => v.Plate == Plaka))
                return new ErrorDataResult<VehicleDto>(Messages.PlateAlreadyExists);

            var bugun = _saat.Bugun;

            var arac = new Vehicle
            {
                CompanyId = user.CompanyId,
                UserId = userId,
                Plate = Plaka,
                Ornek = true,
                Brand = Marka,
                Model = Seri,
                Year = Yil,
                CurrentKm = BaslangicKm + 9000,
                SonKmGuncelleme = DateTime.UtcNow,
                FuelType = FuelType.Dizel,
                KullanimTuru = KullanimTuru.Hususi,
                KasaTipi = Entity.Enums.KasaTipi.Sedan,
                Vites = "Düz",
                Motor = "1.6 Multijet",
                IlkTescilTarihi = new DateTime(Yil, 4, 12),
                CreatedAt = DateTime.UtcNow
            };

            await _vehicleDal.AddAsync(arac);

            await KayitlariEkleAsync(userId, arac.Id, bugun);

            var eklenen = await _vehicleDal.GetAsync(v => v.Id == arac.Id);

            return new SuccessDataResult<VehicleDto>(Esle(eklenen), Messages.OrnekAracEklendi);
        }

        public async Task<IResult> SilAsync(int userId)
        {
            var araclar = await _vehicleDal.GetListAsync(v => v.Ornek);

            foreach (var arac in araclar)
            {
                var sonuc = await _vehicleService.DeleteAsync(userId, arac.Id);
                if (!sonuc.Success)
                    return sonuc;
            }

            return new SuccessResult(Messages.OrnekAracSilindi);
        }

        private async Task KayitlariEkleAsync(int userId, int aracId, DateTime bugun)
        {
            var dolumlar = new[]
            {
                (Gun: 168, Km: BaslangicKm, Litre: 48.4m, Tutar: 2180.50m),
                (Gun: 134, Km: BaslangicKm + 1780, Litre: 47.2m, Tutar: 2144.80m),
                (Gun: 101, Km: BaslangicKm + 3540, Litre: 46.9m, Tutar: 2172.60m),
                (Gun: 66, Km: BaslangicKm + 5410, Litre: 49.6m, Tutar: 2331.20m),
                (Gun: 31, Km: BaslangicKm + 7280, Litre: 48.1m, Tutar: 2308.80m),
                (Gun: 6, Km: BaslangicKm + 9000, Litre: 45.3m, Tutar: 2210.40m)
            };

            foreach (var dolum in dolumlar)
            {
                await _fuelService.AddAsync(userId, new FuelCreateDto
                {
                    VehicleId = aracId,
                    Date = bugun.AddDays(-dolum.Gun),
                    Km = dolum.Km,
                    Liters = dolum.Litre,
                    TotalCost = dolum.Tutar,
                    TamDolum = true
                });
            }

            await _maintenanceService.AddAsync(userId, new MaintenanceCreateDto
            {
                VehicleId = aracId,
                Type = MaintenanceType.PeriyodikBakim,
                Date = bugun.AddDays(-150),
                Km = BaslangicKm + 900,
                Cost = 4850m,
                ServiceName = "Yetkili servis",
                Note = "Yıllık periyodik bakım",
                Parcalar = new List<MaintenancePartDto>
                {
                    new MaintenancePartDto { ParcaTuru = ParcaTuru.MotorYagi, Marka = "Petronas", Aciklama = "5W-30 tam sentetik", Adet = 1 },
                    new MaintenancePartDto { ParcaTuru = ParcaTuru.YagFiltresi, Marka = "Mann", Aciklama = "Yağ filtresi", Adet = 1 },
                    new MaintenancePartDto { ParcaTuru = ParcaTuru.PolenFiltresi, Marka = "Bosch", Aciklama = "Polen filtresi", Adet = 1 }
                }
            });

            await _maintenanceService.AddAsync(userId, new MaintenanceCreateDto
            {
                VehicleId = aracId,
                Type = MaintenanceType.FrenBakimi,
                Date = bugun.AddDays(-48),
                Km = BaslangicKm + 6300,
                Cost = 3120m,
                ServiceName = "Özel servis",
                Note = "Ön balata ve disk değişimi",
                Parcalar = new List<MaintenancePartDto>
                {
                    new MaintenancePartDto { ParcaTuru = ParcaTuru.FrenBalatasiOn, Marka = "Ferodo", Aciklama = "Ön balata takımı", Adet = 1 }
                }
            });

            await _expenseService.AddAsync(userId, new ExpenseCreateDto
            {
                VehicleId = aracId,
                Category = ExpenseCategory.TrafikSigortasi,
                Date = bugun.AddDays(-120),
                Amount = 6400m,
                Note = "Yıllık trafik sigortası"
            });

            await _evrakService.AddAsync(userId, new EvrakCreateDto
            {
                VehicleId = aracId,
                EvrakTuru = EvrakTuru.Muayene,
                BaslangicTarihi = bugun.AddDays(-EvrakKalanGun).AddYears(-2),
                BitisTarihi = bugun.AddDays(EvrakKalanGun),
                Saglayici = "TÜVTÜRK"
            });

            await _lastikService.TakAsync(userId, new LastikTakDto
            {
                VehicleId = aracId,
                Ad = "Yaz seti",
                Mevsim = LastikMevsimi.Yaz,
                Marka = "Michelin",
                Ebat = "205/55 R16",
                DisDerinligiMm = 7.5m,
                TakilmaTarihi = bugun.AddDays(-160),
                TakilmaKm = BaslangicKm
            });
        }

        private static VehicleDto Esle(Vehicle arac)
        {
            return new VehicleDto
            {
                Id = arac.Id,
                Plate = arac.Plate,
                Ornek = arac.Ornek,
                Arsivli = arac.Arsivli,
                Brand = arac.Brand,
                Model = arac.Model,
                Year = arac.Year,
                CurrentKm = arac.CurrentKm,
                FuelType = arac.FuelType,
                KullanimTuru = arac.KullanimTuru,
                IlkTescilTarihi = arac.IlkTescilTarihi,
                KasaTipi = arac.KasaTipi,
                Vites = arac.Vites,
                Motor = arac.Motor,
                SonKmGuncelleme = arac.SonKmGuncelleme,
                CreatedAt = arac.CreatedAt
            };
        }
    }
}
