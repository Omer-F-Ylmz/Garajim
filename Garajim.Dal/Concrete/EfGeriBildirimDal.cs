using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfGeriBildirimDal : EfEntityRepositoryBase<GeriBildirim, GarajimDbContext>, IGeriBildirimDal
    {
        public EfGeriBildirimDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<GeriBildirim>> SonlariAsync(int limit)
        {
            return await Context.GeriBildirimler
                .AsNoTracking()
                .OrderByDescending(g => g.Tarih)
                .ThenByDescending(g => g.Id)
                .Take(limit)
                .ToListAsync();
        }
    }
}
