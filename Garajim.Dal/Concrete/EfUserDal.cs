using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfUserDal : EfEntityRepositoryBase<AppUser, GarajimDbContext>, IUserDal
    {
        public EfUserDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<AppUser> GetForAuthenticationAsync(string email)
        {
            return await Context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> ExistsForRegistrationAsync(string email)
        {
            return await Context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email == email);
        }
    }
}
