using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IImportKaydiDal : IEntityRepository<ImportKaydi>
    {
        Task<HashSet<string>> GetHashesAsync(int vehicleId);
    }
}
