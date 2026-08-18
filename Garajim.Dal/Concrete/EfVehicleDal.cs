using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Concrete
{
    public class EfVehicleDal : EfEntityRepositoryBase<Vehicle, GarajimDbContext>, IVehicleDal
    {
        public EfVehicleDal(GarajimDbContext context) : base(context)
        {
        }
    }
}
