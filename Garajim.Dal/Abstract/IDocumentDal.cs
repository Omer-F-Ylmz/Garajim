using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IDocumentDal : IEntityRepository<Document>
    {
        Task<long> GetCompanyTotalSizeAsync();
    }
}
