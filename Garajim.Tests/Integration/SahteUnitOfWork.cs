using Garajim.Dal.Abstract;

namespace Garajim.Tests.Integration
{
    public sealed class SahteUnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        public int BaslatmaSayisi { get; private set; }

        public int CommitSayisi { get; private set; }

        public Task<IAsyncDisposable> BeginTransactionAsync()
        {
            BaslatmaSayisi++;
            return Task.FromResult<IAsyncDisposable>(this);
        }

        public Task CommitAsync()
        {
            CommitSayisi++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
