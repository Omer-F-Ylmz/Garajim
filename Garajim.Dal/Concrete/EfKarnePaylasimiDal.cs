using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfKarnePaylasimiDal : EfEntityRepositoryBase<KarnePaylasimi, GarajimDbContext>, IKarnePaylasimiDal
    {
        public EfKarnePaylasimiDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<KarnePaylasimi> GetByTokenHashAsync(string tokenHash)
        {
            return await Context.KarnePaylasimlari
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.TokenHash == tokenHash && k.Aktif);
        }

        public async Task PasiflestirAsync(int vehicleId)
        {
            await Context.KarnePaylasimlari
                .Where(k => k.VehicleId == vehicleId && k.Aktif)
                .ExecuteUpdateAsync(setters => setters.SetProperty(k => k.Aktif, false));
        }

        public async Task GoruntulenmeArtirAsync(int id)
        {
            await Context.KarnePaylasimlari
                .IgnoreQueryFilters()
                .Where(k => k.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(k => k.GoruntulenmeSayisi, k => k.GoruntulenmeSayisi + 1));
        }

        public async Task<(int AktifArac, int ToplamGoruntulenme)> IstatistikAsync()
        {
            var aktifler = await Context.KarnePaylasimlari.Where(k => k.Aktif).ToListAsync();
            var tumu = await Context.KarnePaylasimlari.ToListAsync();
            return (aktifler.Select(k => k.VehicleId).Distinct().Count(), tumu.Sum(k => k.GoruntulenmeSayisi));
        }
    }
}
