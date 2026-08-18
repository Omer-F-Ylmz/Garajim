using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Dal.Abstract
{
    public interface IFuelDal : IEntityRepository<FuelRecord>
    {
        Task<decimal> GetTotalCostAsync(int vehicleId, DateTime start, DateTime end);
        Task<List<MonthlyCostDto>> GetMonthlyTotalsAsync(int vehicleId);
    }
}
