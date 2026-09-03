using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfKmDuzeltmeLogDal : EfEntityRepositoryBase<KmDuzeltmeLog, GarajimDbContext>, IKmDuzeltmeLogDal
    {
        public EfKmDuzeltmeLogDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<bool> VarMiAsync(int vehicleId)
        {
            return await Context.KmDuzeltmeLoglari.AsNoTracking().AnyAsync(l => l.VehicleId == vehicleId);
        }
    }
}
