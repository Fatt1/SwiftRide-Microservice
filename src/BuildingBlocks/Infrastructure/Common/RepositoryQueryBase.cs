using System.Linq.Expressions;
using Contracts.Common.Interfaces;
using Contracts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common;

public class RepositoryQueryBase<T, K, TContext> : IRepositoryQueryBase<T, K, TContext>
    where T : EntityBase<K>
    where TContext : DbContext
{
    protected readonly TContext _context;

    public RepositoryQueryBase(TContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IQueryable<T> FindAll(bool trackChanges = false) =>
        !trackChanges ? _context.Set<T>().AsNoTracking() : _context.Set<T>();

    public IQueryable<T> FindAll(bool trackChanges = false, params Expression<Func<T, object>>[] includeProperties)
    {
        var items = FindAll(trackChanges);
        if (includeProperties is { Length: > 0 })
        {
            items = includeProperties.Aggregate(items, (current, includeProperty) => current.Include(includeProperty));
        }
        return items;
    }

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return FindAll(trackChanges).Where(expression);
    }

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false, params Expression<Func<T, object>>[] includeProperties)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return FindAll(trackChanges, includeProperties).Where(expression);
    }

    public async Task<T?> FindByIdAsync(K id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return await FindByCondition(x => x.Id!.Equals(id), trackChanges: false).FirstOrDefaultAsync();
    }

    public async Task<T?> FindByIdAsync(K id, params Expression<Func<T, object>>[] includeProperties)
    {
        ArgumentNullException.ThrowIfNull(id);
        return await FindByCondition(x => x.Id!.Equals(id), trackChanges: false, includeProperties).FirstOrDefaultAsync();
    }
}
