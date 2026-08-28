using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Concrete
{
    public class EfCompanyDal : EfEntityRepositoryBase<Company, GarajimDbContext>, ICompanyDal
    {
        public EfCompanyDal(GarajimDbContext context) : base(context)
        {
        }
    }
}
