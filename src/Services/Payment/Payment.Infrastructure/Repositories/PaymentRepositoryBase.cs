using Contracts.Common.Interfaces;
using Contracts.Domain;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore.Storage;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public abstract class PaymentRepositoryBase<T>(PaymentDbContext context)
    : RepositoryQueryBase<T, Guid, PaymentDbContext>(context), IRepositoryBase<T, Guid, PaymentDbContext>
    where T : EntityBase<Guid>
{
    public Task<Guid> CreateAsync(T entity)
    {
        Stage(entity);
        return Task.FromResult(entity.Id);
    }

    protected void Stage(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Set<T>().Add(entity);
    }

    // Unused operations remain stubs; PaymentUnitOfWork owns saving and transactions.
    public Task<IList<Guid>> CreateRangeAsync(IList<T> entities) => throw new NotImplementedException();
    public Task Update(T entity) => throw new NotImplementedException();
    public Task UpdateRange(IList<T> entities) => throw new NotImplementedException();
    public Task Delete(T entity) => throw new NotImplementedException();
    public Task DeleteRange(IList<T> entities) => throw new NotImplementedException();
    public Task<int> SaveChangesAsync() => throw new NotImplementedException();
    public Task<IDbContextTransaction> BeginTransactionAsync() => throw new NotImplementedException();
    public Task EndTransactionAsync() => throw new NotImplementedException();
    public Task RollBackTransactionAsync() => throw new NotImplementedException();
}
