using Garajim.Business.Abstract;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public class VehicleAccessManager : IVehicleAccessService
    {
        private readonly IUserDal _userDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IVehicleAssignmentDal _assignmentDal;

        public VehicleAccessManager(IUserDal userDal, IVehicleDal vehicleDal, IVehicleAssignmentDal assignmentDal)
        {
            _userDal = userDal;
            _vehicleDal = vehicleDal;
            _assignmentDal = assignmentDal;
        }

        public async Task<Vehicle> GetAccessibleAsync(int userId, int vehicleId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
            {
                return null;
            }

            var vehicle = await _vehicleDal.GetAsync(v => v.Id == vehicleId);
            if (vehicle == null)
            {
                return null;
            }

            if (user.Role != CompanyRole.Driver)
            {
                return vehicle;
            }

            var zimmetli = await _assignmentDal.GetActiveVehicleIdsAsync(userId);
            return zimmetli.Contains(vehicleId) ? vehicle : null;
        }

        public async Task<List<Vehicle>> GetAccessibleListAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
            {
                return new List<Vehicle>();
            }

            if (user.Role != CompanyRole.Driver)
            {
                return await _vehicleDal.GetListAsync();
            }

            var zimmetli = await _assignmentDal.GetActiveVehicleIdsAsync(userId);
            if (zimmetli.Count == 0)
            {
                return new List<Vehicle>();
            }

            return await _vehicleDal.GetListAsync(v => zimmetli.Contains(v.Id));
        }
    }
}
