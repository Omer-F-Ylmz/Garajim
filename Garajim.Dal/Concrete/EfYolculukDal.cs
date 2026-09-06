using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Dal.Sorgular;
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
        public Task<SayfaliSonuc<YolculukKaydi>> SayfaAsync(List<int> vehicleIds, ListeSorgusu sorgu, SiralamaSonucu siralama)
        {
            var sorgulama = Context.YolculukKayitlari.AsNoTracking().Where(y => vehicleIds.Contains(y.VehicleId));
            var metin = TurkceArama.Iceren<YolculukKaydi>(sorgu.GecerliQ(), y => y.Nereden, y => y.Nereye, y => y.Not);

            return Sayfalayici.UygulaAsync(sorgulama, sorgu, siralama, y => y.Tarih, metin, Sirala);
        }

        private static IQueryable<YolculukKaydi> Sirala(IQueryable<YolculukKaydi> sorgulama, SiralamaSonucu siralama)
        {
            return siralama.Alan switch
            {
                "mesafe" => siralama.Artan
                    ? sorgulama.OrderBy(y => y.MesafeKm).ThenBy(y => y.Id)
                    : sorgulama.OrderByDescending(y => y.MesafeKm).ThenByDescending(y => y.Id),
                "amac" => siralama.Artan
                    ? sorgulama.OrderBy(y => y.Amac).ThenBy(y => y.Id)
                    : sorgulama.OrderByDescending(y => y.Amac).ThenByDescending(y => y.Id),
                _ => siralama.Artan
                    ? sorgulama.OrderBy(y => y.Tarih).ThenBy(y => y.Id)
                    : sorgulama.OrderByDescending(y => y.Tarih).ThenByDescending(y => y.Id)
            };
        }
    }
}

