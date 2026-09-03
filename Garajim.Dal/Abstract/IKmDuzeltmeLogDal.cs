using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IKmDuzeltmeLogDal : IEntityRepository<KmDuzeltmeLog>
    {
        Task<bool> VarMiAsync(int vehicleId);
    }
}
