using System.Linq.Expressions;
using Garajim.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Core.DataAccess.EntityFramework
{
    public class EfEntityRepositoryBase<TEntity, TContext> : IEntityRepository<TEntity>
        where TEntity : class, IEntity, new()
        where TContext : DbContext
    {
        protected readonly TContext Context;

        public EfEntityRepositoryBase(TContext context)
        {
            Context = context;
        }

        public async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await Context.Set<TEntity>().FirstOrDefaultAsync(predicate);
        }

        public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            IQueryable<TEntity> query = Context.Set<TEntity>().AsNoTracking();
            if (predicate != null)
                query = query.Where(predicate);
            return await query.ToListAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await Context.Set<TEntity>().AnyAsync(predicate);
        }

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            IQueryable<TEntity> query = Context.Set<TEntity>();
            if (predicate != null)
                query = query.Where(predicate);
            return await query.CountAsync();
        }

        public async Task AddAsync(TEntity entity)
        {
            await Context.Set<TEntity>().AddAsync(entity);
            await Context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TEntity entity)
        {
            Context.Set<TEntity>().Update(entity);
            await Context.SaveChangesAsync();
        }

        public async Task DeleteAsync(TEntity entity)
        {
            Context.Set<TEntity>().Remove(entity);
            await Context.SaveChangesAsync();
        }
    }
}
