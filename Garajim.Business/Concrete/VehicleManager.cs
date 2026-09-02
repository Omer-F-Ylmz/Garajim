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

        public VehicleManager(IVehicleDal vehicleDal, IUserDal userDal, IVehicleAccessService vehicleAccess, ICompanyDal companyDal, PlanKurallari planKurallari)
        {
            _vehicleDal = vehicleDal;
            _userDal = userDal;
            _vehicleAccess = vehicleAccess;
            _companyDal = companyDal;
            _planKurallari = planKurallari;
        }

        public async Task<IDataResult<List<VehicleDto>>> GetAllAsync(int userId)
        {
            var vehicles = await _vehicleAccess.GetAccessibleListAsync(userId);
            var list = vehicles.OrderBy(v => v.Plate).Select(MapToDto).ToList();
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
                string.IsNullOrWhiteSpace(dto.Model) || dto.Year < 1950 ||
                dto.Year > DateTime.UtcNow.Year + 1 || dto.CurrentKm < 0 || !Enum.IsDefined(dto.FuelType))
                return new ErrorDataResult<VehicleDto>(Messages.InvalidValue);
            var plate = dto.Plate.Trim().ToUpperInvariant().Replace(" ", "");
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
            if (await _vehicleDal.CountAsync(v => v.CompanyId == owner.CompanyId) >= limit)
                return new ErrorDataResult<VehicleDto>(Messages.AracLimitiAsildi);
            var vehicle = new Vehicle
            {
                CompanyId = owner.CompanyId,
                UserId = userId,
                Plate = plate,
                Brand = dto.Brand.Trim(),
                Model = dto.Model.Trim(),
                Year = dto.Year,
                CurrentKm = dto.CurrentKm,
                FuelType = dto.FuelType,
                KullanimTuru = Enum.IsDefined(dto.KullanimTuru) ? dto.KullanimTuru : KullanimTuru.Hususi,
                IlkTescilTarihi = dto.IlkTescilTarihi?.Date,
                AcilKisiAd = dto.AcilKisiAd,
                AcilKisiTelefon = dto.AcilKisiTelefon,
                AcilNot = dto.AcilNot,
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
                dto.Year < 1950 || dto.Year > DateTime.UtcNow.Year + 1 || dto.CurrentKm < 0 || !Enum.IsDefined(dto.FuelType))
                return new ErrorResult(Messages.InvalidValue);
            vehicle.Brand = dto.Brand.Trim();
            vehicle.Model = dto.Model.Trim();
            vehicle.Year = dto.Year;
            vehicle.CurrentKm = dto.CurrentKm;
            vehicle.FuelType = dto.FuelType;
            vehicle.KullanimTuru = Enum.IsDefined(dto.KullanimTuru) ? dto.KullanimTuru : vehicle.KullanimTuru;
            vehicle.IlkTescilTarihi = dto.IlkTescilTarihi?.Date ?? vehicle.IlkTescilTarihi;
            vehicle.AcilKisiAd = dto.AcilKisiAd;
            vehicle.AcilKisiTelefon = dto.AcilKisiTelefon;
            vehicle.AcilNot = dto.AcilNot;
            await _vehicleDal.UpdateAsync(vehicle);
            return new SuccessResult(Messages.VehicleUpdated);
        }

        public async Task<IResult> DeleteAsync(int userId, int id)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, id);
            if (vehicle == null)
                return new ErrorResult(Messages.VehicleNotFound);
            await _vehicleDal.DeleteAsync(vehicle);
            return new SuccessResult(Messages.VehicleDeleted);
        }

        private static VehicleDto MapToDto(Vehicle vehicle)
        {
            return new VehicleDto
            {
                Id = vehicle.Id,
                Plate = vehicle.Plate,
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Year = vehicle.Year,
                CurrentKm = vehicle.CurrentKm,
                FuelType = vehicle.FuelType,
                CreatedAt = vehicle.CreatedAt
            };
        }
    }
}
