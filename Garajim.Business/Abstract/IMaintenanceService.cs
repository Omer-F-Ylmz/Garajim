using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IMaintenanceService
    {
        Task<IDataResult<List<MaintenanceDto>>> GetListAsync(int userId, int vehicleId);
        Task<IDataResult<MaintenanceDto>> AddAsync(int userId, MaintenanceCreateDto dto);
        Task<IDataResult<MaintenanceDto>> UpdateAsync(int userId, int id, MaintenanceUpdateDto dto);
        Task<IResult> DeleteAsync(int userId, int id);
    }
}
