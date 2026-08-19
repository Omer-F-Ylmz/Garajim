using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Dal.Abstract
{
    public interface IMaintenanceDal : IEntityRepository<MaintenanceRecord>
    {
        Task<decimal> GetTotalCostAsync(int vehicleId, DateTime start, DateTime end);
        Task<List<MonthlyCostDto>> GetMonthlyTotalsAsync(int vehicleId);
        Task<List<MaintenanceRecord>> GetRecentAsync(int vehicleId, int limit);
    }
}
