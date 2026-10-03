using Contracts.Common.Interfaces;
using Contracts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Common;

public class RepositoryBase<T, K, TContext> : RepositoryQueryBase<T, K, TContext>, IRepositoryBase<T, K, TContext>
    where T : EntityBase<K>
    where TContext : DbContext
{
    public RepositoryBase(TContext context) : base(context)
    {
    }

    public async Task<K> CreateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<IList<K>> CreateRangeAsync(IList<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await _context.Set<T>().AddRangeAsync(entities);
        await _context.SaveChangesAsync();
        return entities.Select(x => x.Id).ToList();
    }

    public Task Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public Task UpdateRange(IList<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        _context.Set<T>().UpdateRange(entities);
        return Task.CompletedTask;
    }

    public Task Delete(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteRange(IList<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        _context.Set<T>().RemoveRange(entities);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync() =>
        await _context.SaveChangesAsync();

    public async Task<IDbContextTransaction> BeginTransactionAsync() =>
        await _context.Database.BeginTransactionAsync();

    public async Task EndTransactionAsync()
    {
        await _context.SaveChangesAsync();
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CommitTransactionAsync();
        }
    }

    public async Task RollBackTransactionAsync()
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.RollbackTransactionAsync();
        }
    }
}
