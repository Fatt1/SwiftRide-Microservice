using EventBus.Messages.Common;

namespace EventBus.Messages.Events;

public record TripMatchingFailedEvent : IntegrationBaseEvent
{
    public Guid TripId { get; init; }
}
