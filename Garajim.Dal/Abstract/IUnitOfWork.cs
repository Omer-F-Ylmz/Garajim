namespace Garajim.Dal.Abstract
{
    public interface IUnitOfWork
    {
        Task<IAsyncDisposable> BeginTransactionAsync();

        Task CommitAsync();
    }
}
