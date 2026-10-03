using Contracts.Domain.Interfaces;

namespace Contracts.Domain;

public abstract class EntityBase<Tkey> : IEntityBase<Tkey>
{
    public Tkey Id { get; set; }
}
