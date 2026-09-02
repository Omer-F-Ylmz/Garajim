using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfAracDegerDal : EfEntityRepositoryBase<AracDeger, GarajimDbContext>, IAracDegerDal
    {
        public EfAracDegerDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<AracDeger>> GetSeriAsync(int vehicleId, int limit)
        {
            return await Context.AracDegerleri
                .AsNoTracking()
                .Where(d => d.VehicleId == vehicleId)
                .OrderByDescending(d => d.Tarih)
                .ThenByDescending(d => d.Id)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<AracDeger> SonDegerAsync(int vehicleId)
        {
            return await Context.AracDegerleri
                .AsNoTracking()
                .Where(d => d.VehicleId == vehicleId)
                .OrderByDescending(d => d.Tarih)
                .ThenByDescending(d => d.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GunlukTahminSayisiAsync(int vehicleId, DateTime gun)
        {
            var ertesi = gun.Date.AddDays(1);

            return await Context.AracDegerleri
                .AsNoTracking()
                .CountAsync(d => d.VehicleId == vehicleId
                                 && d.Kaynak == DegerKaynagi.Tahmin
                                 && d.OlusturmaTarihi >= gun.Date
                                 && d.OlusturmaTarihi < ertesi);
        }

        public async Task<decimal> FiloToplamSonDegerAsync(List<int> vehicleIds)
        {
            if (vehicleIds.Count == 0)
            {
                return 0m;
            }

            var sonIdler = await Context.AracDegerleri
                .AsNoTracking()
                .Where(d => vehicleIds.Contains(d.VehicleId))
                .GroupBy(d => d.VehicleId)
                .Select(g => g.OrderByDescending(d => d.Tarih).ThenByDescending(d => d.Id).Select(d => d.Id).First())
                .ToListAsync();

            if (sonIdler.Count == 0)
            {
                return 0m;
            }

            return await Context.AracDegerleri
                .AsNoTracking()
                .Where(d => sonIdler.Contains(d.Id))
                .SumAsync(d => d.Deger);
        }

        public async Task<AracDeger> AraliktakiIlkAsync(int vehicleId, DateTime baslangic, DateTime bitis)
        {
            return await Context.AracDegerleri
                .AsNoTracking()
                .Where(d => d.VehicleId == vehicleId && d.Tarih >= baslangic && d.Tarih <= bitis)
                .OrderBy(d => d.Tarih)
                .ThenBy(d => d.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<AracDeger> AraliktakiSonAsync(int vehicleId, DateTime baslangic, DateTime bitis)
        {
            return await Context.AracDegerleri
                .AsNoTracking()
                .Where(d => d.VehicleId == vehicleId && d.Tarih >= baslangic && d.Tarih <= bitis)
                .OrderByDescending(d => d.Tarih)
                .ThenByDescending(d => d.Id)
                .FirstOrDefaultAsync();
        }
    }
}
