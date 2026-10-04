using EventBus.Messages.Common;

namespace EventBus.Messages.Events;



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

public record 


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
