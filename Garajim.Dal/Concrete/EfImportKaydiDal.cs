using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfImportKaydiDal : EfEntityRepositoryBase<ImportKaydi, GarajimDbContext>, IImportKaydiDal
    {
        public EfImportKaydiDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<HashSet<string>> GetHashesAsync(int vehicleId)
        {
            var hashler = await Context.ImportKayitlari
                .AsNoTracking()
                .Where(i => i.VehicleId == vehicleId)
                .Select(i => i.SatirHash)
                .ToListAsync();

            return hashler.ToHashSet();
        }
    }
}
