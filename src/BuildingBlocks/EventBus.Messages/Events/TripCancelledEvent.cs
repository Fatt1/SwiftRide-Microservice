using EventBus.Messages.Common;
namespace EventBus.Messages.Events;
public record TripCancelledEvent : IntegrationBaseEvent
{
    public Guid TripId { get; init; }
    public Guid? DriverId { get; init; }
}
