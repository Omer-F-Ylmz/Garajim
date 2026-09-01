using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IMaintenancePartDal : IEntityRepository<MaintenancePart>
    {
        Task<List<MaintenancePart>> GetByVehicleAsync(int vehicleId);
        Task DeleteByRecordAsync(int maintenanceRecordId);
    }
}
