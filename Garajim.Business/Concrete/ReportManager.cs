using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Concrete
{
    public class ReportManager : IReportService
    {
        private readonly IVehicleDal _vehicleDal;
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IFuelDal _fuelDal;
        private readonly IExpenseDal _expenseDal;

        public ReportManager(IVehicleDal vehicleDal, IMaintenanceDal maintenanceDal, IFuelDal fuelDal, IExpenseDal expenseDal)
        {
            _vehicleDal = vehicleDal;
            _maintenanceDal = maintenanceDal;
            _fuelDal = fuelDal;
            _expenseDal = expenseDal;
        }

        public async Task<IDataResult<ExpenseSummaryDto>> GetSummaryAsync(int userId, int vehicleId, DateTime start, DateTime end)
        {
            var vehicle = await _vehicleDal.GetAsync(v => v.Id == vehicleId && v.UserId == userId);
            if (vehicle == null)
                return new ErrorDataResult<ExpenseSummaryDto>(Messages.VehicleNotFound);
            if (end < start)
                return new ErrorDataResult<ExpenseSummaryDto>(Messages.InvalidValue);
            var endInclusive = end.Date >= DateTime.MaxValue.Date
                ? DateTime.MaxValue
                : end.Date.AddDays(1).AddTicks(-1);
            var totalFuel = await _fuelDal.GetTotalCostAsync(vehicleId, start, endInclusive);
            var totalMaintenance = await _maintenanceDal.GetTotalCostAsync(vehicleId, start, endInclusive);
            var categories = await _expenseDal.GetCategoryTotalsAsync(vehicleId, start, endInclusive);
            var totalOther = categories.Sum(c => c.Total);
            var summary = new ExpenseSummaryDto
            {
                TotalFuel = totalFuel,
                TotalMaintenance = totalMaintenance,
                TotalOtherExpense = totalOther,
                GrandTotal = totalFuel + totalMaintenance + totalOther,
                Categories = categories.OrderByDescending(c => c.Total).ToList()
            };
            return new SuccessDataResult<ExpenseSummaryDto>(summary);
        }

        public async Task<IDataResult<List<MonthlyCostDto>>> GetMonthlyAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleDal.GetAsync(v => v.Id == vehicleId && v.UserId == userId);
            if (vehicle == null)
                return new ErrorDataResult<List<MonthlyCostDto>>(Messages.VehicleNotFound);
            var merged = new Dictionary<(int Year, int Month), decimal>();
            foreach (var item in await _fuelDal.GetMonthlyTotalsAsync(vehicleId))
                Accumulate(merged, item);
            foreach (var item in await _maintenanceDal.GetMonthlyTotalsAsync(vehicleId))
                Accumulate(merged, item);
            foreach (var item in await _expenseDal.GetMonthlyTotalsAsync(vehicleId))
                Accumulate(merged, item);
            var list = merged
                .Select(kv => new MonthlyCostDto { Year = kv.Key.Year, Month = kv.Key.Month, Total = kv.Value })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();
            return new SuccessDataResult<List<MonthlyCostDto>>(list);
        }

        public async Task<IDataResult<FuelStatsDto>> GetFuelStatsAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleDal.GetAsync(v => v.Id == vehicleId && v.UserId == userId);
            if (vehicle == null)
                return new ErrorDataResult<FuelStatsDto>(Messages.VehicleNotFound);
            var records = (await _fuelDal.GetListAsync(f => f.VehicleId == vehicleId && f.Km > 0))
                .OrderBy(f => f.Km)
                .ToList();
            if (records.Count < 2)
                return new ErrorDataResult<FuelStatsDto>(Messages.NotEnoughFuelData);
            var totalKm = records[records.Count - 1].Km - records[0].Km;
            if (totalKm <= 0)
                return new ErrorDataResult<FuelStatsDto>(Messages.NotEnoughFuelData);
            var consumedLiters = records.Skip(1).Sum(f => f.Liters);
            var consumedCost = records.Skip(1).Sum(f => f.TotalCost);
            var stats = new FuelStatsDto
            {
                TotalKm = totalKm,
                TotalLiters = records.Sum(f => f.Liters),
                TotalCost = records.Sum(f => f.TotalCost),
                AverageConsumptionPer100Km = Math.Round(consumedLiters / totalKm * 100, 2),
                CostPerKm = Math.Round(consumedCost / totalKm, 2)
            };
            return new SuccessDataResult<FuelStatsDto>(stats);
        }

        private static void Accumulate(Dictionary<(int Year, int Month), decimal> merged, MonthlyCostDto item)
        {
            var key = (item.Year, item.Month);
            merged.TryGetValue(key, out var current);
            merged[key] = current + item.Total;
        }
    }
}
