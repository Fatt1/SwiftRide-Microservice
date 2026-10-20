using EventBus.Messages.Common;

namespace EventBus.Messages.Events;

/// <summary>
/// Published when Matching Service finds a driver for a requested trip.
/// Notification Service consumes this event and pushes a notification to Driver via WebSocket.
/// </summary>
public record DriverFoundEvent : IntegrationBaseEvent
{
    public Guid TripId { get; init; }
    public Guid DriverId { get; init; }
    public string DriverName { get; init; } = string.Empty;
    public Guid? RiderId { get; init; }
    public string PickupAddress { get; init; } = string.Empty;
    public string DropoffAddress { get; init; } = string.Empty;
    public decimal TotalFare { get; init; }
    public double EstimatedDurationMinutes { get; init; }
    public double DistanceKm { get; init; }
}
