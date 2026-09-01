using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfLastikDal : EfEntityRepositoryBase<LastikSeti, GarajimDbContext>, ILastikDal
    {
        public EfLastikDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<LastikSeti>> GetListeAsync(int vehicleId)
        {
            return await Context.LastikSetleri
                .AsNoTracking()
                .Where(l => l.VehicleId == vehicleId)
                .OrderByDescending(l => l.Takili)
                .ThenByDescending(l => l.TakilmaTarihi)
                .ThenByDescending(l => l.Id)
                .ToListAsync();
        }

        public async Task<LastikSeti> GetTakiliAsync(int vehicleId)
        {
            return await Context.LastikSetleri
                .FirstOrDefaultAsync(l => l.VehicleId == vehicleId && l.Takili);
        }
    }
}
