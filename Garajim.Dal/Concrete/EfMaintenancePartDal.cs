using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfMaintenancePartDal : EfEntityRepositoryBase<MaintenancePart, GarajimDbContext>, IMaintenancePartDal
    {
        public EfMaintenancePartDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<MaintenancePart>> GetByVehicleAsync(int vehicleId)
        {
            return await Context.MaintenanceParts
                .AsNoTracking()
                .Where(p => p.VehicleId == vehicleId)
                .ToListAsync();
        }

        public async Task DeleteByRecordAsync(int maintenanceRecordId)
        {
            await Context.MaintenanceParts
                .Where(p => p.MaintenanceRecordId == maintenanceRecordId)
                .ExecuteDeleteAsync();
        }
    }
}
