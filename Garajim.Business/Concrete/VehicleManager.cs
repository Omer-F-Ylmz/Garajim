using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Concrete
{
    public class VehicleManager : IVehicleService
    {
        private readonly IVehicleDal _vehicleDal;
        private readonly IUserDal _userDal;

        public VehicleManager(IVehicleDal vehicleDal, IUserDal userDal)
        {
            _vehicleDal = vehicleDal;
            _userDal = userDal;
        }

        public async Task<IDataResult<List<VehicleDto>>> GetAllAsync(int userId)
        {
            var vehicles = await _vehicleDal.GetListAsync(v => v.UserId == userId);
            var list = vehicles.OrderBy(v => v.Plate).Select(MapToDto).ToList();
            return new SuccessDataResult<List<VehicleDto>>(list);
        }

        public async Task<IDataResult<VehicleDto>> GetByIdAsync(int userId, int id)
        {
            var vehicle = await _vehicleDal.GetAsync(v => v.Id == id && v.UserId == userId);
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
            if (await _vehicleDal.AnyAsync(v => v.UserId == userId && v.Plate == plate))
                return new ErrorDataResult<VehicleDto>(Messages.PlateAlreadyExists);
            var owner = await _userDal.GetAsync(u => u.Id == userId);
            if (owner == null)
                return new ErrorDataResult<VehicleDto>(Messages.InvalidValue);
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
                CreatedAt = DateTime.UtcNow
            };
            await _vehicleDal.AddAsync(vehicle);
            return new SuccessDataResult<VehicleDto>(MapToDto(vehicle), Messages.VehicleAdded);
        }

        public async Task<IResult> UpdateAsync(int userId, int id, VehicleUpdateDto dto)
        {
            var vehicle = await _vehicleDal.GetAsync(v => v.Id == id && v.UserId == userId);
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
            await _vehicleDal.UpdateAsync(vehicle);
            return new SuccessResult(Messages.VehicleUpdated);
        }

        public async Task<IResult> DeleteAsync(int userId, int id)
        {
            var vehicle = await _vehicleDal.GetAsync(v => v.Id == id && v.UserId == userId);
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
