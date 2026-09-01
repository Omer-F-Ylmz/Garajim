using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfTakvimAbonelikDal : EfEntityRepositoryBase<TakvimAbonelik, GarajimDbContext>, ITakvimAbonelikDal
    {
        public EfTakvimAbonelikDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<TakvimAbonelik> GetByTokenHashAsync(string tokenHash)
        {
            return await Context.TakvimAbonelikleri
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.Aktif);
        }

        public async Task PasiflestirAsync(int userId)
        {
            await Context.TakvimAbonelikleri
                .Where(t => t.UserId == userId && t.Aktif)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Aktif, false));
        }
    }
}
