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

        public async Task SupheliGuncelleAsync(int vehicleId, IReadOnlyCollection<int> supheliIdler)
        {
            var kayitlar = await Context.FuelRecords.Where(f => f.VehicleId == vehicleId).ToListAsync();
            var degisti = false;

            foreach (var kayit in kayitlar)
            {
                var supheli = supheliIdler.Contains(kayit.Id);
                if (kayit.SupheliKm != supheli)
                {
                    kayit.SupheliKm = supheli;
                    degisti = true;
                }
            }

            if (degisti)
            {
                await Context.SaveChangesAsync();
            }
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
                .Select(g => new MonthlyCostDto { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(x => (decimal?)x.TotalCost) ?? 0 })
                .ToListAsync();
        }
        public async Task<List<MonthlyCostDto>> GetMonthlyTotalsAsync(int vehicleId, DateTime start, DateTime end)
        {
            return await Context.FuelRecords
                .AsNoTracking()
                .Where(f => f.VehicleId == vehicleId && f.Date >= start && f.Date <= end)
                .GroupBy(f => new { f.Date.Year, f.Date.Month })
                .Select(g => new MonthlyCostDto { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(x => (decimal?)x.TotalCost) ?? 0 })
                .ToListAsync();
        }

        public async Task<List<YakitOlcumDto>> GetOlcumlerAsync(int vehicleId, DateTime start, DateTime end)
        {
            return await Context.FuelRecords
                .AsNoTracking()
                .Where(f => f.VehicleId == vehicleId && f.Km > 0 && !f.SupheliKm && f.Date >= start && f.Date <= end)
                .OrderBy(f => f.Km).ThenBy(f => f.Id)
                .Select(f => new YakitOlcumDto { Tarih = f.Date, Km = f.Km, Litre = f.Liters, Kwh = f.Kwh ?? 0m, TamDolum = f.TamDolum })
                .ToListAsync();
        }

        public async Task<List<AracYakitOlcumDto>> GetOlcumlerAsync(List<int> vehicleIds, DateTime start, DateTime end)
        {
            return await Context.FuelRecords
                .AsNoTracking()
                .Where(f => vehicleIds.Contains(f.VehicleId) && f.Km > 0 && !f.SupheliKm && f.Date >= start && f.Date <= end)
                .OrderBy(f => f.VehicleId).ThenBy(f => f.Km).ThenBy(f => f.Id)
                .Select(f => new AracYakitOlcumDto
                {
                    VehicleId = f.VehicleId,
                    Tarih = f.Date,
                    Km = f.Km,
                    Litre = f.Liters,
                    Kwh = f.Kwh ?? 0m,
                    TamDolum = f.TamDolum
                })
                .ToListAsync();
        }

        public async Task<List<AracToplamDto>> GetTotalsByVehicleAsync(List<int> vehicleIds, DateTime start, DateTime end)
        {
            return await Context.FuelRecords
                .AsNoTracking()
                .Where(f => vehicleIds.Contains(f.VehicleId) && f.Date >= start && f.Date <= end)
                .GroupBy(f => f.VehicleId)
                .Select(g => new AracToplamDto { VehicleId = g.Key, Toplam = g.Sum(x => (decimal?)x.TotalCost) ?? 0 })
                .ToListAsync();
        }

        public async Task<List<FuelRecord>> GetRecentAsync(int vehicleId, int limit)
        {
            return await Context.FuelRecords
                .AsNoTracking()
                .Where(f => f.VehicleId == vehicleId)
                .OrderByDescending(f => f.Date)
                .ThenByDescending(f => f.Id)
                .Take(limit)
                .ToListAsync();
        }
    }
}
