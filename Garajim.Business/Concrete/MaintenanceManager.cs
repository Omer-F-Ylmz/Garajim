using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Concrete
{
    public class MaintenanceManager : IMaintenanceService
    {
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IVehicleAccessService _vehicleAccess;

        public MaintenanceManager(IMaintenanceDal maintenanceDal, IVehicleDal vehicleDal, IVehicleAccessService vehicleAccess)
        {
            _maintenanceDal = maintenanceDal;
            _vehicleDal = vehicleDal;
            _vehicleAccess = vehicleAccess;
        }

        public async Task<IDataResult<List<MaintenanceDto>>> GetListAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<List<MaintenanceDto>>(Messages.VehicleNotFound);
            var records = await _maintenanceDal.GetRecentAsync(vehicleId, QueryLimits.MaxListSize);
            var list = records.Select(MapToDto).ToList();
            return new SuccessDataResult<List<MaintenanceDto>>(list);
        }

        public async Task<IDataResult<MaintenanceDto>> AddAsync(int userId, MaintenanceCreateDto dto)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<MaintenanceDto>(Messages.VehicleNotFound);
            if (dto.Cost < 0 || dto.Km < 0 || !Enum.IsDefined(dto.Type))
                return new ErrorDataResult<MaintenanceDto>(Messages.InvalidValue);
            var record = new MaintenanceRecord
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = dto.VehicleId,
                Type = dto.Type,
                Date = dto.Date,
                Km = dto.Km,
                Cost = dto.Cost,
                ServiceName = dto.ServiceName,
                Note = dto.Note
            };
            await _maintenanceDal.AddAsync(record);
            if (dto.Km > vehicle.CurrentKm)
            {
                vehicle.CurrentKm = dto.Km;
                await _vehicleDal.UpdateAsync(vehicle);
            }
            return new SuccessDataResult<MaintenanceDto>(MapToDto(record), Messages.RecordAdded);
        }

        public async Task<IResult> DeleteAsync(int userId, int id)
        {
            var record = await _maintenanceDal.GetAsync(m => m.Id == id);
            if (record == null)
                return new ErrorResult(Messages.RecordNotFound);
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, record.VehicleId);
            if (vehicle == null)
                return new ErrorResult(Messages.RecordNotFound);
            await _maintenanceDal.DeleteAsync(record);
            return new SuccessResult(Messages.RecordDeleted);
        }

        private static MaintenanceDto MapToDto(MaintenanceRecord record)
        {
            return new MaintenanceDto
            {
                Id = record.Id,
                VehicleId = record.VehicleId,
                Type = record.Type,
                Date = record.Date,
                Km = record.Km,
                Cost = record.Cost,
                ServiceName = record.ServiceName,
                Note = record.Note
            };
        }
    }
}
