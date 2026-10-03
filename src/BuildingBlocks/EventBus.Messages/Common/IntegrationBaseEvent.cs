namespace EventBus.Messages.Common;

public interface IIntegrationEvent
{
    Guid Id { get; }
    Guid CorrelationId { get; }
    DateTimeOffset CreationDate { get; }
}

public abstract record IntegrationBaseEvent : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public DateTimeOffset CreationDate { get; init; } = DateTimeOffset.UtcNow;
}
