using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IVehicleAssignmentDal : IEntityRepository<VehicleAssignment>
    {
        Task<List<int>> GetActiveVehicleIdsAsync(int userId);

        Task<VehicleAssignment> GetActiveByVehicleAsync(int vehicleId);

        Task<List<VehicleAssignment>> GetHistoryAsync(int vehicleId);
    }
}
