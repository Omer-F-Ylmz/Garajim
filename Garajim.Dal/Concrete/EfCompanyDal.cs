using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfCompanyDal : EfEntityRepositoryBase<Company, GarajimDbContext>, ICompanyDal
    {
        public EfCompanyDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<Company> GetByDavetKoduAsync(string kod)
        {
            return await Context.Companies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.DavetKodu == kod);
        }

        public async Task<bool> DavetKoduVarMiAsync(string kod)
        {
            return await Context.Companies
                .IgnoreQueryFilters()
                .AnyAsync(c => c.DavetKodu == kod);
        }

        public async Task<List<Company>> GetDavetlilerAsync(int companyId)
        {
            return await Context.Companies
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.DavetEdenCompanyId == companyId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
