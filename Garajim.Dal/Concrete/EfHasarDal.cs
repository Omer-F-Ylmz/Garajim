using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
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

        public async Task<int> SonSiraAsync(int hasarDosyasiId)
        {
            return await Context.HasarFotograflari
                .Where(f => f.HasarDosyasiId == hasarDosyasiId)
                .Select(f => (int?)f.Sira)
                .MaxAsync() ?? 0;
        }
    }
}
