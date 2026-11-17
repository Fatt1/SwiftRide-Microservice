using EventBus.Messages.Common;

namespace EventBus.Messages.Events;

/// <summary>
/// Published when Matching Service finds a driver for a requested trip.
/// </summary>
public record DriverFoundEvent : IntegrationBaseEvent
{
    public Guid TripId { get; init; }
    public string MatchSessionId { get; init; } = "";
    public Guid DriverId { get; init; }
    public string DriverName { get; init; } = string.Empty;
    public decimal TotalFare { get; init; }
    public double EstimatedDurationMinutes { get; init; }

    public int MaxAllowedRejections { get; init; }

    public double DistanceKm { get; init; }
}
