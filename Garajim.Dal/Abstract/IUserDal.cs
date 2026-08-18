using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IUserDal : IEntityRepository<AppUser>
    {
    }
}
