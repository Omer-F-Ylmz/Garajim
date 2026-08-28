using Garajim.Entity.Concrete;

namespace Garajim.Business.Abstract
{
    public interface IVehicleAccessService
    {
        Task<Vehicle> GetAccessibleAsync(int userId, int vehicleId);

        Task<List<Vehicle>> GetAccessibleListAsync(int userId);
    }
}
