using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfReceiptDraftDal : EfEntityRepositoryBase<ReceiptDraft, GarajimDbContext>, IReceiptDraftDal
    {
        public EfReceiptDraftDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<int> BekleyenSayisiAsync()
        {
            return await Context.ReceiptDrafts
                .AsNoTracking()
                .CountAsync(d => d.Durum == ReceiptDraftStatus.Bekliyor);
        }

        public async Task<int> GetMonthlyCountAsync(DateTime ayBasi)
        {
            return await Context.ReceiptDrafts
                .AsNoTracking()
                .CountAsync(d => d.OlusturmaTarihi >= ayBasi);
        }
    }
}
