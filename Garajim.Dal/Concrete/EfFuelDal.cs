using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfFuelDal : EfEntityRepositoryBase<FuelRecord, GarajimDbContext>, IFuelDal
    {
        public EfFuelDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<decimal> GetTotalCostAsync(int vehicleId, DateTime start, DateTime end)
        {
            return await Context.FuelRecords
                .Where(f => f.VehicleId == vehicleId && f.Date >= start && f.Date <= end)
                .SumAsync(f => (decimal?)f.TotalCost) ?? 0;
        }

        public async Task<List<MonthlyCostDto>> GetMonthlyTotalsAsync(int vehicleId)
        {
            return await Context.FuelRecords
                .Where(f => f.VehicleId == vehicleId)
                .GroupBy(f => new { f.Date.Year, f.Date.Month })
                .Select(g => new MonthlyCostDto { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(x => x.TotalCost) })
                .ToListAsync();
        }
    }
}
