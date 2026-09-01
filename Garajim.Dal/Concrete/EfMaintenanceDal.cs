using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfMaintenanceDal : EfEntityRepositoryBase<MaintenanceRecord, GarajimDbContext>, IMaintenanceDal
    {
        public EfMaintenanceDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<decimal> GetTotalCostAsync(int vehicleId, DateTime start, DateTime end)
        {
            return await Context.MaintenanceRecords
                .Where(m => m.VehicleId == vehicleId && m.Date >= start && m.Date <= end)
                .SumAsync(m => (decimal?)m.Cost) ?? 0;
        }

        public async Task<List<MonthlyCostDto>> GetMonthlyTotalsAsync(int vehicleId)
        {
            return await Context.MaintenanceRecords
                .Where(m => m.VehicleId == vehicleId)
                .GroupBy(m => new { m.Date.Year, m.Date.Month })
                .Select(g => new MonthlyCostDto { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(x => (decimal?)x.Cost) ?? 0 })
                .ToListAsync();
        }
        public async Task<List<MonthlyCostDto>> GetMonthlyTotalsAsync(int vehicleId, DateTime start, DateTime end)
        {
            return await Context.MaintenanceRecords
                .AsNoTracking()
                .Where(m => m.VehicleId == vehicleId && m.Date >= start && m.Date <= end)
                .GroupBy(m => new { m.Date.Year, m.Date.Month })
                .Select(g => new MonthlyCostDto { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(x => (decimal?)x.Cost) ?? 0 })
                .ToListAsync();
        }

        public async Task<List<AracToplamDto>> GetTotalsByVehicleAsync(List<int> vehicleIds, DateTime start, DateTime end)
        {
            return await Context.MaintenanceRecords
                .AsNoTracking()
                .Where(m => vehicleIds.Contains(m.VehicleId) && m.Date >= start && m.Date <= end)
                .GroupBy(m => m.VehicleId)
                .Select(g => new AracToplamDto { VehicleId = g.Key, Toplam = g.Sum(x => (decimal?)x.Cost) ?? 0 })
                .ToListAsync();
        }

        public async Task<List<MaintenanceRecord>> GetRecentAsync(int vehicleId, int limit)
        {
            return await Context.MaintenanceRecords
                .AsNoTracking()
                .Where(m => m.VehicleId == vehicleId)
                .OrderByDescending(m => m.Date)
                .ThenByDescending(m => m.Id)
                .Take(limit)
                .ToListAsync();
        }
    }
}
