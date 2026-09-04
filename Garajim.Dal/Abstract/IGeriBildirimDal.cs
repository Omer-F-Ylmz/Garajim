using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IGeriBildirimDal : IEntityRepository<GeriBildirim>
    {
        Task<List<GeriBildirim>> SonlariAsync(int limit);
    }
}
