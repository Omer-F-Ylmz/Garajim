using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IReceiptDraftDal : IEntityRepository<ReceiptDraft>
    {
        Task<int> GetMonthlyCountAsync(DateTime ayBasi);
        Task<int> BekleyenSayisiAsync();
    }
}
