using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfVehicleAssignmentDal : EfEntityRepositoryBase<VehicleAssignment, GarajimDbContext>, IVehicleAssignmentDal
    {
        public EfVehicleAssignmentDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<int>> GetActiveVehicleIdsAsync(int userId)
        {
            return await Context.VehicleAssignments
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.EndDate == null)
                .Select(a => a.VehicleId)
                .ToListAsync();
        }

        public async Task<VehicleAssignment> GetActiveByVehicleAsync(int vehicleId)
        {
            return await Context.VehicleAssignments
                .FirstOrDefaultAsync(a => a.VehicleId == vehicleId && a.EndDate == null);
        }

        public async Task<int> AktifSayiAsync(List<int> vehicleIds)
        {
            return await Context.VehicleAssignments
                .AsNoTracking()
                .CountAsync(a => a.EndDate == null && vehicleIds.Contains(a.VehicleId));
        }

        public async Task<List<VehicleAssignment>> GetHistoryAsync(int vehicleId)
        {
            return await Context.VehicleAssignments
                .AsNoTracking()
                .Where(a => a.VehicleId == vehicleId)
                .OrderByDescending(a => a.StartDate)
                .ThenByDescending(a => a.Id)
                .ToListAsync();
        }
    }
}
