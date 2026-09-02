using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface ILastikDal : IEntityRepository<LastikSeti>
    {
        Task<List<LastikSeti>> GetListeAsync(int vehicleId, int limit);
        Task<LastikSeti> GetTakiliAsync(int vehicleId);
        Task<List<LastikSeti>> GetTakiliListeAsync(List<int> vehicleIds);
    }
}
