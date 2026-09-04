using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Dal.Abstract
{
    public interface IFuelDal : IEntityRepository<FuelRecord>
    {
        Task<decimal> GetTotalCostAsync(int vehicleId, DateTime start, DateTime end);
        Task SupheliGuncelleAsync(int vehicleId, IReadOnlyCollection<int> supheliIdler);
        Task<List<MonthlyCostDto>> GetMonthlyTotalsAsync(int vehicleId);
        Task<List<MonthlyCostDto>> GetMonthlyTotalsAsync(int vehicleId, DateTime start, DateTime end);
        Task<List<YakitOlcumDto>> GetOlcumlerAsync(int vehicleId, DateTime start, DateTime end);
        Task<List<AracYakitOlcumDto>> GetOlcumlerAsync(List<int> vehicleIds, DateTime start, DateTime end);
        Task<List<AracToplamDto>> GetTotalsByVehicleAsync(List<int> vehicleIds, DateTime start, DateTime end);
        Task<List<FuelRecord>> GetRecentAsync(int vehicleId, int limit);
    }
}
