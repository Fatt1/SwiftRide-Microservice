using Contracts.Domain.Interfaces;

namespace Contracts.Domain;

public class EntityAuditableBase<TKey> : EntityBase<TKey>, IAuditable
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastModifiedAt { get; set; }
}
