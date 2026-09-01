using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IReportService
    {
        Task<IDataResult<ExpenseSummaryDto>> GetSummaryAsync(int userId, int vehicleId, DateTime start, DateTime end);
        Task<IDataResult<List<MonthlyCostDto>>> GetMonthlyAsync(int userId, int vehicleId);
        Task<IDataResult<FuelStatsDto>> GetFuelStatsAsync(int userId, int vehicleId);
        Task<IDataResult<AracMaliyetDto>> GetAracMaliyetAsync(int userId, int vehicleId, DateTime baslangic, DateTime bitis);
        Task<IDataResult<FiloMaliyetDto>> GetFiloMaliyetAsync(int userId, DateTime baslangic, DateTime bitis);
        Task<IDataResult<DashboardDto>> GetDashboardAsync(int userId);
    }
}
