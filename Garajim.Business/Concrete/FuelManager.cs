using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Concrete
{
    public class FuelManager : IFuelService
    {
        private readonly IFuelDal _fuelDal;
        private readonly IVehicleDal _vehicleDal;

        public FuelManager(IFuelDal fuelDal, IVehicleDal vehicleDal)
        {
            _fuelDal = fuelDal;
            _vehicleDal = vehicleDal;
        }

        public async Task<IDataResult<List<FuelDto>>> GetListAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleDal.GetAsync(v => v.Id == vehicleId && v.UserId == userId);
            if (vehicle == null)
                return new ErrorDataResult<List<FuelDto>>(Messages.VehicleNotFound);
            var records = await _fuelDal.GetRecentAsync(vehicleId, QueryLimits.MaxListSize);
            var list = records.Select(MapToDto).ToList();
            return new SuccessDataResult<List<FuelDto>>(list);
        }

        public async Task<IDataResult<FuelDto>> AddAsync(int userId, FuelCreateDto dto)
        {
            var vehicle = await _vehicleDal.GetAsync(v => v.Id == dto.VehicleId && v.UserId == userId);
            if (vehicle == null)
                return new ErrorDataResult<FuelDto>(Messages.VehicleNotFound);
            if (dto.Liters <= 0 || dto.TotalCost < 0 || dto.Km < 0)
                return new ErrorDataResult<FuelDto>(Messages.InvalidValue);
            var record = new FuelRecord
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = dto.VehicleId,
                Date = dto.Date,
                Liters = dto.Liters,
                TotalCost = dto.TotalCost,
                Km = dto.Km
            };
            await _fuelDal.AddAsync(record);
            if (dto.Km > vehicle.CurrentKm)
            {
                vehicle.CurrentKm = dto.Km;
                await _vehicleDal.UpdateAsync(vehicle);
            }
            return new SuccessDataResult<FuelDto>(MapToDto(record), Messages.RecordAdded);
        }

        public async Task<IResult> DeleteAsync(int userId, int id)
        {
            var record = await _fuelDal.GetAsync(f => f.Id == id);
            if (record == null)
                return new ErrorResult(Messages.RecordNotFound);
            var vehicle = await _vehicleDal.GetAsync(v => v.Id == record.VehicleId && v.UserId == userId);
            if (vehicle == null)
                return new ErrorResult(Messages.RecordNotFound);
            await _fuelDal.DeleteAsync(record);
            return new SuccessResult(Messages.RecordDeleted);
        }

        private static FuelDto MapToDto(FuelRecord record)
        {
            return new FuelDto
            {
                Id = record.Id,
                VehicleId = record.VehicleId,
                Date = record.Date,
                Liters = record.Liters,
                TotalCost = record.TotalCost,
                Km = record.Km
            };
        }
    }
}
