using EventBus.Messages.Common;

namespace EventBus.Messages.Events;

/// <summary>
/// Published by Trip Service when a rider requests a trip.
/// </summary>
public record TripRequestedEvent : IntegrationBaseEvent
{
    public Guid TripId { get; init; }
    public Guid RiderId { get; init; }
    public double PickupLat { get; init; }
    public double PickupLng { get; init; }
    public double DropoffLat { get; init; }
    public double DropoffLng { get; init; }
    public string PickupAddress { get; init; } = default!;
    public string DropoffAddress { get; init; } = default!;
}

/// <summary>
/// Published by Matching Service when a driver accepts the trip offer.
/// </summary>
public record DriverMatchedEvent : IntegrationBaseEvent
{
    public Guid TripId { get; init; }
    public Guid DriverId { get; init; }
    public decimal EstimatedFare { get; init; }
    public string QuoteId { get; init; } = default!;
}

/// <summary>
/// Published by Trip Service when rider is dropped off.
/// Triggers Payment Service to charge the rider and credit the driver.
/// </summary>
public record RideCompletedEvent : IntegrationBaseEvent
{
    public Guid TripId { get; init; }
    public Guid RiderId { get; init; }
    public Guid DriverId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = default!; // "Wallet" | "Card"
    public string? GatewayToken { get; init; }
    public string Currency { get; init; } = "VND";
}

/// <summary>
/// Published by Payment Service when payment succeeds.
/// Trip Service listens to transition status to Paid.
/// </summary>
public record PaymentCompletedEvent : IntegrationBaseEvent
{
    public Guid PaymentId { get; init; }
    public Guid TripId { get; init; }
    public Guid RiderId { get; init; }
    public Guid DriverId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = default!;
}

/// <summary>
/// Published by Payment Service when payment fails.
/// </summary>
public record PaymentFailedEvent : IntegrationBaseEvent
{
    public Guid TripId { get; init; }
    public Guid RiderId { get; init; }
    public decimal Amount { get; init; }
    public string Reason { get; init; } = default!;
}
