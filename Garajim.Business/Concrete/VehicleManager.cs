using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Planlar;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public class VehicleManager : IVehicleService
    {
        private readonly IVehicleDal _vehicleDal;
        private readonly IUserDal _userDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly ICompanyDal _companyDal;
        private readonly PlanKurallari _planKurallari;
        private readonly IKmDuzeltmeLogDal _kmLogDal;

        public VehicleManager(IVehicleDal vehicleDal, IUserDal userDal, IVehicleAccessService vehicleAccess, ICompanyDal companyDal, PlanKurallari planKurallari, IKmDuzeltmeLogDal kmLogDal)
        {
            _vehicleDal = vehicleDal;
            _userDal = userDal;
            _vehicleAccess = vehicleAccess;
            _companyDal = companyDal;
            _planKurallari = planKurallari;
            _kmLogDal = kmLogDal;
        }

        public async Task<IDataResult<List<VehicleDto>>> GetAllAsync(int userId, bool arsiv = false)
        {
            var vehicles = await _vehicleAccess.GetAccessibleListAsync(userId);
            var list = vehicles.Where(v => v.Arsivli == arsiv).OrderBy(v => v.Plate).Select(MapToDto).ToList();
            return new SuccessDataResult<List<VehicleDto>>(list);
        }

        public async Task<IDataResult<VehicleDto>> GetByIdAsync(int userId, int id)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, id);
            if (vehicle == null)
                return new ErrorDataResult<VehicleDto>(Messages.VehicleNotFound);
            return new SuccessDataResult<VehicleDto>(MapToDto(vehicle));
        }

        public async Task<IDataResult<VehicleDto>> AddAsync(int userId, VehicleCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Plate) || string.IsNullOrWhiteSpace(dto.Brand) ||
                string.IsNullOrWhiteSpace(dto.Model) || !DegerSinirlari.YilGecerli(dto.Year) ||
                !DegerSinirlari.KmGecerli(dto.CurrentKm) || !Enum.IsDefined(dto.FuelType))
                return new ErrorDataResult<VehicleDto>(Messages.InvalidValue);
            var plate = PlakaDogrulayici.Normalize(dto.Plate);
            if (plate.Length > AracAlanUzunluklari.Plaka)
                return new ErrorDataResult<VehicleDto>(Messages.InvalidValue);
            if (!PlakaDogrulayici.Gecerli(dto.Plate, dto.YabanciPlaka))
                return new ErrorDataResult<VehicleDto>(dto.YabanciPlaka ? Messages.YabanciPlakaGecersiz : Messages.PlakaGecersiz);
            if (await _vehicleDal.AnyAsync(v => v.Plate == plate))
                return new ErrorDataResult<VehicleDto>(Messages.PlateAlreadyExists);
            var owner = await _userDal.GetAsync(u => u.Id == userId);
            if (owner == null)
                return new ErrorDataResult<VehicleDto>(Messages.InvalidValue);
            var sirket = await _companyDal.GetAsync(c => c.Id == owner.CompanyId);
            if (sirket == null)
                return new ErrorDataResult<VehicleDto>(Messages.InvalidValue);
            var davetSayisi = await _companyDal.DavetSayisiAsync(sirket.Id);
            var limit = _planKurallari.AracLimiti(sirket.PlanType, sirket.AracLimiti, davetSayisi);
            if (await _vehicleDal.CountAsync(v => v.CompanyId == owner.CompanyId && !v.Arsivli) >= limit)
                return new ErrorDataResult<VehicleDto>(Messages.AracLimitiAsildi);
            var vehicle = new Vehicle
            {
                CompanyId = owner.CompanyId,
                UserId = userId,
                Plate = plate,
                YabanciPlaka = dto.YabanciPlaka,
                Brand = Kirp(dto.Brand, AracAlanUzunluklari.Marka),
                Model = Kirp(dto.Model, AracAlanUzunluklari.Model),
                Year = dto.Year,
                CurrentKm = dto.CurrentKm,
                FuelType = dto.FuelType,
                KullanimTuru = Enum.IsDefined(dto.KullanimTuru) ? dto.KullanimTuru : KullanimTuru.Hususi,
                IlkTescilTarihi = dto.IlkTescilTarihi?.Date,
                KasaTipi = dto.KasaTipi != null && Enum.IsDefined(dto.KasaTipi.Value) ? dto.KasaTipi : null,
                Vites = Kirp(dto.Vites, AracAlanUzunluklari.Vites),
                Motor = Kirp(dto.Motor, AracAlanUzunluklari.Motor),
                AcilKisiAd = Kirp(dto.AcilKisiAd, AracAlanUzunluklari.AcilKisiAd),
                AcilKisiTelefon = Kirp(dto.AcilKisiTelefon, AracAlanUzunluklari.AcilKisiTelefon),
                AcilNot = Kirp(dto.AcilNot, AracAlanUzunluklari.AcilNot),
                CreatedAt = DateTime.UtcNow
            };
            await _vehicleDal.AddAsync(vehicle);
            return new SuccessDataResult<VehicleDto>(MapToDto(vehicle), Messages.VehicleAdded);
        }

        public async Task<IResult> UpdateAsync(int userId, int id, VehicleUpdateDto dto)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, id);
            if (vehicle == null)
                return new ErrorResult(Messages.VehicleNotFound);
            if (string.IsNullOrWhiteSpace(dto.Brand) || string.IsNullOrWhiteSpace(dto.Model) ||
                !DegerSinirlari.YilGecerli(dto.Year) || !DegerSinirlari.KmGecerli(dto.CurrentKm) || !Enum.IsDefined(dto.FuelType))
                return new ErrorResult(Messages.InvalidValue);
            vehicle.Brand = Kirp(dto.Brand, AracAlanUzunluklari.Marka);
            vehicle.Model = Kirp(dto.Model, AracAlanUzunluklari.Model);
            vehicle.Year = dto.Year;
            if (dto.CurrentKm < vehicle.CurrentKm)
            {
                var neden = (dto.KmDuzeltmeNedeni ?? string.Empty).Trim();

                if (dto.KmDusurmeOnayi != true || neden.Length < 3)
                    return new ErrorResult(Messages.KmDusurmeOnayiGerekli);

                await _kmLogDal.AddAsync(new KmDuzeltmeLog
                {
                    CompanyId = vehicle.CompanyId,
                    VehicleId = vehicle.Id,
                    UserId = userId,
                    EskiKm = vehicle.CurrentKm,
                    YeniKm = dto.CurrentKm,
                    Neden = Kirp(neden, 200),
                    Tarih = DateTime.UtcNow
                });
            }

            vehicle.CurrentKm = dto.CurrentKm;
            vehicle.FuelType = dto.FuelType;
            vehicle.KullanimTuru = dto.KullanimTuru != null && Enum.IsDefined(dto.KullanimTuru.Value) ? dto.KullanimTuru.Value : vehicle.KullanimTuru;
            vehicle.IlkTescilTarihi = dto.IlkTescilTarihi?.Date ?? vehicle.IlkTescilTarihi;
            vehicle.KasaTipi = dto.KasaTipi != null && Enum.IsDefined(dto.KasaTipi.Value) ? dto.KasaTipi : vehicle.KasaTipi;
            vehicle.Vites = Kirp(dto.Vites, AracAlanUzunluklari.Vites) ?? vehicle.Vites;
            vehicle.Motor = Kirp(dto.Motor, AracAlanUzunluklari.Motor) ?? vehicle.Motor;
            vehicle.AcilKisiAd = Kirp(dto.AcilKisiAd, AracAlanUzunluklari.AcilKisiAd);
            vehicle.AcilKisiTelefon = Kirp(dto.AcilKisiTelefon, AracAlanUzunluklari.AcilKisiTelefon);
            vehicle.AcilNot = Kirp(dto.AcilNot, AracAlanUzunluklari.AcilNot);
            await _vehicleDal.UpdateAsync(vehicle);
            return new SuccessResult(Messages.VehicleUpdated);
        }

        public async Task<IResult> KasaTipiSecAsync(int userId, int id, KasaTipi kasaTipi)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, id);
            if (vehicle == null)
                return new ErrorResult(Messages.VehicleNotFound);
            if (!Enum.IsDefined(kasaTipi))
                return new ErrorResult(Messages.InvalidValue);
            vehicle.KasaTipi = kasaTipi;
            await _vehicleDal.UpdateAsync(vehicle);
            return new SuccessResult(Messages.VehicleUpdated);
        }

        public async Task<IResult> ArsivleAsync(int userId, int id, ArsivNedeni neden)
        {
            if (!Enum.IsDefined(neden))
                return new ErrorResult(Messages.InvalidValue);

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, id);
            if (vehicle == null)
                return new ErrorResult(Messages.VehicleNotFound);

            if (vehicle.Arsivli)
                return new SuccessResult(Messages.AracArsivlendi);

            vehicle.Arsivli = true;
            vehicle.ArsivNedeni = neden;
            vehicle.ArsivTarihi = DateTime.UtcNow;
            await _vehicleDal.UpdateAsync(vehicle);

            return new SuccessResult(Messages.AracArsivlendi);
        }

        public async Task<IResult> ArsivdenAlAsync(int userId, int id)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, id);
            if (vehicle == null)
                return new ErrorResult(Messages.VehicleNotFound);

            if (!vehicle.Arsivli)
                return new SuccessResult(Messages.AracArsivdenAlindi);

            var sirket = await _companyDal.GetAsync(c => c.Id == vehicle.CompanyId);
            if (sirket == null)
                return new ErrorResult(Messages.InvalidValue);

            var davetSayisi = await _companyDal.DavetSayisiAsync(sirket.Id);
            var limit = _planKurallari.AracLimiti(sirket.PlanType, sirket.AracLimiti, davetSayisi);

            if (await _vehicleDal.CountAsync(v => v.CompanyId == vehicle.CompanyId && !v.Arsivli) >= limit)
                return new ErrorResult(Messages.AracLimitiAsildi);

            vehicle.Arsivli = false;
            vehicle.ArsivNedeni = null;
            vehicle.ArsivTarihi = null;
            await _vehicleDal.UpdateAsync(vehicle);

            return new SuccessResult(Messages.AracArsivdenAlindi);
        }

        public async Task<IResult> DeleteAsync(int userId, int id)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, id);
            if (vehicle == null)
                return new ErrorResult(Messages.VehicleNotFound);
            await _vehicleDal.DeleteAsync(vehicle);
            return new SuccessResult(Messages.VehicleDeleted);
        }

        private static string Kirp(string metin, int uzunluk)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return null;
            }

            var kirpik = metin.Trim();
            return kirpik.Length > uzunluk ? kirpik.Substring(0, uzunluk) : kirpik;
        }

        private static VehicleDto MapToDto(Vehicle vehicle)
        {
            return new VehicleDto
            {
                Id = vehicle.Id,
                Plate = vehicle.Plate,
                YabanciPlaka = vehicle.YabanciPlaka,
                Arsivli = vehicle.Arsivli,
                ArsivNedeni = vehicle.ArsivNedeni?.ToString(),
                ArsivTarihi = vehicle.ArsivTarihi,
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Year = vehicle.Year,
                CurrentKm = vehicle.CurrentKm,
                FuelType = vehicle.FuelType,
                KullanimTuru = vehicle.KullanimTuru,
                IlkTescilTarihi = vehicle.IlkTescilTarihi,
                KasaTipi = vehicle.KasaTipi,
                Vites = vehicle.Vites,
                Motor = vehicle.Motor,
                AcilKisiAd = vehicle.AcilKisiAd,
                AcilKisiTelefon = vehicle.AcilKisiTelefon,
                AcilNot = vehicle.AcilNot,
                CreatedAt = vehicle.CreatedAt
            };
        }
    }
}
