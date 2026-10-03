using System.Linq.Expressions;
using Contracts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Contracts.Common.Interfaces;

public interface IRepositoryQueryBase<T, K, TContext>
    where T : EntityBase<K>
    where TContext : DbContext
{

    IQueryable<T> FindAll(bool trackChanges = false);

    IQueryable<T> FindAll(bool trackChanges = false, params Expression<Func<T, object>>[] includeProperties);


    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false);

    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false, params Expression<Func<T, object>>[] includeProperties);


    Task<T?> FindByIdAsync(K id);

    Task<T?> FindByIdAsync(K id, params Expression<Func<T, object>>[] includeProperties);
}


public interface IRepositoryBase<T, K, TContext> : IRepositoryQueryBase<T, K, TContext>
    where T : EntityBase<K>
    where TContext : DbContext
{
    Task<K> CreateAsync(T entity);
    Task<IList<K>> CreateRangeAsync(IList<T> entities);

    Task Update(T entity);

    Task UpdateRange(IList<T> entities);

    Task Delete(T entity);

    Task DeleteRange(IList<T> entities);

    Task<int> SaveChangesAsync();

    Task<IDbContextTransaction> BeginTransactionAsync();

    Task EndTransactionAsync();

    Task RollBackTransactionAsync();
}
