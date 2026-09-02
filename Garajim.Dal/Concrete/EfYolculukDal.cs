using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfYolculukDal : EfEntityRepositoryBase<YolculukKaydi, GarajimDbContext>, IYolculukDal
    {
        public EfYolculukDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<YolculukKaydi>> GetListeAsync(List<int> vehicleIds, DateTime baslangic, DateTime bitis, int limit)
        {
            return await Context.YolculukKayitlari
                .AsNoTracking()
                .Where(y => vehicleIds.Contains(y.VehicleId) && y.Tarih >= baslangic && y.Tarih <= bitis)
                .OrderByDescending(y => y.Tarih)
                .ThenByDescending(y => y.Id)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<AmacToplamDto>> AmacToplamlariAsync(List<int> vehicleIds, DateTime baslangic, DateTime bitis)
        {
            return await Context.YolculukKayitlari
                .AsNoTracking()
                .Where(y => vehicleIds.Contains(y.VehicleId) && y.Tarih >= baslangic && y.Tarih <= bitis)
                .GroupBy(y => y.Amac)
                .Select(g => new AmacToplamDto { Amac = g.Key, ToplamKm = g.Sum(x => x.MesafeKm), Adet = g.Count() })
                .ToListAsync();
        }
    }
}
