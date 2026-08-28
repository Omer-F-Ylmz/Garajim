using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfDocumentDal : EfEntityRepositoryBase<Document, GarajimDbContext>, IDocumentDal
    {
        public EfDocumentDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<long> GetCompanyTotalSizeAsync()
        {
            return await Context.Documents.SumAsync(d => (long?)d.SizeBytes) ?? 0;
        }
    }
}
