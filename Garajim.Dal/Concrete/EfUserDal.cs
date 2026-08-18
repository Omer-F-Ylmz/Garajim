using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Concrete
{
    public class EfUserDal : EfEntityRepositoryBase<AppUser, GarajimDbContext>, IUserDal
    {
        public EfUserDal(GarajimDbContext context) : base(context)
        {
        }
    }
}
