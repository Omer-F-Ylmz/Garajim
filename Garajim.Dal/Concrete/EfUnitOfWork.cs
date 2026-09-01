using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace Garajim.Dal.Concrete
{
    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly GarajimDbContext _context;
        private IDbContextTransaction _transaction;

        public EfUnitOfWork(GarajimDbContext context)
        {
            _context = context;
        }

        public async Task<IAsyncDisposable> BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
            return _transaction;
        }

        public async Task CommitAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
    }
}
