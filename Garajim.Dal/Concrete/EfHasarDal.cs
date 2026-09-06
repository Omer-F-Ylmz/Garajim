using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Dal.Sorgular;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfHasarDosyasiDal : EfEntityRepositoryBase<HasarDosyasi, GarajimDbContext>, IHasarDosyasiDal
    {
        public EfHasarDosyasiDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<HasarDosyasi>> GetListeAsync(List<int> vehicleIds, int limit)
        {
            return await Context.HasarDosyalari
                .AsNoTracking()
                .Where(h => vehicleIds.Contains(h.VehicleId))
                .OrderByDescending(h => h.OlayTarihi)
                .ThenByDescending(h => h.Id)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> AcikSayisiAsync(List<int> vehicleIds)
        {
            return await Context.HasarDosyalari
                .AsNoTracking()
                .CountAsync(h => vehicleIds.Contains(h.VehicleId) && h.Durum != HasarDurumu.Kapandi);
        }
        public Task<SayfaliSonuc<HasarDosyasi>> SayfaAsync(List<int> vehicleIds, ListeSorgusu sorgu, SiralamaSonucu siralama)
        {
            var sorgulama = Context.HasarDosyalari.AsNoTracking().Where(h => vehicleIds.Contains(h.VehicleId));
            var metin = TurkceArama.Iceren<HasarDosyasi>(sorgu.GecerliQ(),
                h => h.Aciklama, h => h.Konum, h => h.KarsiTarafPlaka, h => h.SigortaDosyaNo);

            return Sayfalayici.UygulaAsync(sorgulama, sorgu, siralama, h => h.OlayTarihi, metin, Sirala);
        }

        private static IQueryable<HasarDosyasi> Sirala(IQueryable<HasarDosyasi> sorgulama, SiralamaSonucu siralama)
        {
            return siralama.Alan switch
            {
                "durum" => siralama.Artan
                    ? sorgulama.OrderBy(h => h.Durum).ThenBy(h => h.Id)
                    : sorgulama.OrderByDescending(h => h.Durum).ThenByDescending(h => h.Id),
                "bedel" => siralama.Artan
                    ? sorgulama.OrderBy(h => h.HasarBedeli).ThenBy(h => h.Id)
                    : sorgulama.OrderByDescending(h => h.HasarBedeli).ThenByDescending(h => h.Id),
                _ => siralama.Artan
                    ? sorgulama.OrderBy(h => h.OlayTarihi).ThenBy(h => h.Id)
                    : sorgulama.OrderByDescending(h => h.OlayTarihi).ThenByDescending(h => h.Id)
            };
        }
    }

    public class EfHasarFotoDal : EfEntityRepositoryBase<HasarFoto, GarajimDbContext>, IHasarFotoDal
    {
        public EfHasarFotoDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<HasarFoto>> GetByDosyaAsync(int hasarDosyasiId)
        {
            return await Context.HasarFotograflari
                .AsNoTracking()
                .Where(f => f.HasarDosyasiId == hasarDosyasiId)
                .OrderBy(f => f.Sira)
                .ThenBy(f => f.Id)
                .ToListAsync();
        }

        public async Task<int> SayiAsync(int hasarDosyasiId)
        {
            return await Context.HasarFotograflari.CountAsync(f => f.HasarDosyasiId == hasarDosyasiId);
        }

        public async Task<Dictionary<int, int>> SayilarAsync(List<int> hasarDosyasiIdleri)
        {
            if (hasarDosyasiIdleri == null || hasarDosyasiIdleri.Count == 0)
            {
                return new Dictionary<int, int>();
            }

            return await Context.HasarFotograflari
                .Where(f => hasarDosyasiIdleri.Contains(f.HasarDosyasiId))
                .GroupBy(f => f.HasarDosyasiId)
                .Select(g => new { g.Key, Sayi = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Sayi);
        }

        public async Task<List<int>> AracinFotoBelgeIdleriAsync(int vehicleId)

        {
            return await (from foto in Context.HasarFotograflari
                          join dosya in Context.HasarDosyalari on foto.HasarDosyasiId equals dosya.Id
                          where dosya.VehicleId == vehicleId
                          select foto.DocumentId).ToListAsync();
        }

        public async Task<int> SonSiraAsync(int hasarDosyasiId)
        {
            return await Context.HasarFotograflari
                .Where(f => f.HasarDosyasiId == hasarDosyasiId)
                .Select(f => (int?)f.Sira)
                .MaxAsync() ?? 0;
        }
    }
}
