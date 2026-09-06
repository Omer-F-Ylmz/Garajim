using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Sorgular;
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
        public async Task<SayfaliSonuc<MaintenanceRecord>> SayfaAsync(int vehicleId, ListeSorgusu sorgu, SiralamaSonucu siralama, List<int> parcaEslesenIdler)
        {
            var sorgulama = Context.MaintenanceRecords.AsNoTracking().Where(m => m.VehicleId == vehicleId);

            if (sorgu.Baslangic != null)
            {
                var bas = sorgu.Baslangic.Value.Date;
                sorgulama = sorgulama.Where(m => m.Date >= bas);
            }

            if (sorgu.Bitis != null)
            {
                var son = sorgu.Bitis.Value.Date;
                sorgulama = sorgulama.Where(m => m.Date <= son);
            }

            var terim = sorgu.GecerliQ();

            if (terim != null)
            {
                var metin = TurkceArama.Iceren<MaintenanceRecord>(terim, m => m.ServiceName, m => m.Note);
                var idler = parcaEslesenIdler ?? new List<int>();
                var suzgec = idler.Count == 0 ? metin : Ifadeler.Veya(metin, m => idler.Contains(m.Id));

                sorgulama = sorgulama.Where(suzgec);
            }

            var toplam = await sorgulama.CountAsync();

            sorgulama = siralama.Alan switch
            {
                "km" => siralama.Artan
                    ? sorgulama.OrderBy(m => m.Km).ThenBy(m => m.Id)
                    : sorgulama.OrderByDescending(m => m.Km).ThenByDescending(m => m.Id),
                "tutar" => siralama.Artan
                    ? sorgulama.OrderBy(m => m.Cost).ThenBy(m => m.Id)
                    : sorgulama.OrderByDescending(m => m.Cost).ThenByDescending(m => m.Id),
                "servis" => siralama.Artan
                    ? sorgulama.OrderBy(m => m.ServiceName).ThenBy(m => m.Id)
                    : sorgulama.OrderByDescending(m => m.ServiceName).ThenByDescending(m => m.Id),
                _ => siralama.Artan
                    ? sorgulama.OrderBy(m => m.Date).ThenBy(m => m.Id)
                    : sorgulama.OrderByDescending(m => m.Date).ThenByDescending(m => m.Id)
            };

            var sayfaNo = sorgu.GecerliSayfa();
            var boyut = sorgu.GecerliBoyut();

            var kayitlar = await sorgulama.Skip((sayfaNo - 1) * boyut).Take(boyut).ToListAsync();

            return new SayfaliSonuc<MaintenanceRecord>(kayitlar, toplam, sayfaNo, boyut);
        }
    }
}

