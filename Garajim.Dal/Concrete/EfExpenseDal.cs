using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfExpenseDal : EfEntityRepositoryBase<ExpenseRecord, GarajimDbContext>, IExpenseDal
    {
        public EfExpenseDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<decimal> GetTotalCostAsync(int vehicleId, DateTime start, DateTime end)
        {
            return await Context.ExpenseRecords
                .Where(e => e.VehicleId == vehicleId && e.Date >= start && e.Date <= end)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;
        }

        public async Task<List<MonthlyCostDto>> GetMonthlyTotalsAsync(int vehicleId)
        {
            return await Context.ExpenseRecords
                .Where(e => e.VehicleId == vehicleId)
                .GroupBy(e => new { e.Date.Year, e.Date.Month })
                .Select(g => new MonthlyCostDto { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(x => x.Amount) })
                .ToListAsync();
        }

        public async Task<List<CategoryTotalDto>> GetCategoryTotalsAsync(int vehicleId, DateTime start, DateTime end)
        {
            var totals = await Context.ExpenseRecords
                .Where(e => e.VehicleId == vehicleId && e.Date >= start && e.Date <= end)
                .GroupBy(e => e.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount) })
                .ToListAsync();
            return totals.Select(t => new CategoryTotalDto { Category = t.Category.ToString(), Total = t.Total }).ToList();
        }
        public async Task<List<ExpenseRecord>> GetRecentAsync(int vehicleId, int limit)
        {
            return await Context.ExpenseRecords
                .AsNoTracking()
                .Where(e => e.VehicleId == vehicleId)
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .Take(limit)
                .ToListAsync();
        }
    }
}
