using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IMaintenancePartDal : IEntityRepository<MaintenancePart>
    {
        Task<List<int>> AciklamaEslesenBakimIdleriAsync(int vehicleId, string terim);
        Task<List<MaintenancePart>> KayitlaraGoreAsync(int vehicleId, List<int> kayitIdleri);
        Task TopluEkleAsync(List<MaintenancePart> parcalar);
        Task<List<MaintenancePart>> GetByVehicleAsync(int vehicleId);
        Task DeleteByRecordAsync(int maintenanceRecordId);
    }
}
