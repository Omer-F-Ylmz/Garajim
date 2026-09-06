using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Sorgular;
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

        public async Task TopluEkleAsync(List<MaintenancePart> parcalar)
        {
            if (parcalar == null || parcalar.Count == 0)
            {
                return;
            }

            await Context.MaintenanceParts.AddRangeAsync(parcalar);
            await Context.SaveChangesAsync();
        }

        public async Task DeleteByRecordAsync(int maintenanceRecordId)
        {
            await Context.MaintenanceParts
                .Where(p => p.MaintenanceRecordId == maintenanceRecordId)
                .ExecuteDeleteAsync();
        }
        public async Task<List<int>> AciklamaEslesenBakimIdleriAsync(int vehicleId, string terim)
        {
            var suzgec = TurkceArama.Iceren<MaintenancePart>(terim, p => p.Aciklama, p => p.Marka);

            if (suzgec == null)
            {
                return new List<int>();
            }

            return await Context.MaintenanceParts
                .AsNoTracking()
                .Where(p => p.VehicleId == vehicleId)
                .Where(suzgec)
                .Select(p => p.MaintenanceRecordId)
                .Distinct()
                .ToListAsync();
        }
    }
}

