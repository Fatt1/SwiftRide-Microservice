using Contracts.Domain;
using Infrastructure.Common;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public abstract class PaymentRepositoryBase<T>(PaymentDbContext context)
    : RepositoryBase<T, Guid, PaymentDbContext>(context)
    where T : EntityBase<Guid>
{
    protected void Stage(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Set<T>().Add(entity);
    }

}
