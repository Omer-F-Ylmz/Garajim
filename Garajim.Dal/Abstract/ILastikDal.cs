using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface ILastikDal : IEntityRepository<LastikSeti>
    {
        Task<List<LastikSeti>> GetListeAsync(int vehicleId);
        Task<LastikSeti> GetTakiliAsync(int vehicleId);
    }
}
