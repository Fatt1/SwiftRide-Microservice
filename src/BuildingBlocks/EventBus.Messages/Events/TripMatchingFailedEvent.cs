using EventBus.Messages.Common;

namespace EventBus.Messages.Events;

public record TripMatchingFailedEvent : IntegrationBaseEvent
{
    public string Reason { get; init; } = "No available drivers.";
    public Guid TripId { get; init; }
    public string MatchSessionId { get; init; } = "";

}
