using EventBus.Messages.Common;

namespace EventBus.Messages.Events;

/// <summary>
/// Published when a driver rejects a trip proposal.
/// Notification Service consumes this event and notifies the Rider via WebSocket.
/// </summary>
public record DriverRejectEvent : IntegrationBaseEvent
{
    public Guid TripId { get; init; }
    public Guid DriverId { get; init; }
    public Guid? RiderId { get; init; }
    public string Reason { get; init; } = "Tài xế đã từ chối chuyến đi";
}
