using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface ITakvimAbonelikDal : IEntityRepository<TakvimAbonelik>
    {
        Task<TakvimAbonelik> GetByTokenHashAsync(string tokenHash);
        Task PasiflestirAsync(int userId);
    }
}
