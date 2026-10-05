using EventBus.Messages.Common;
using Shared.Enums.Payments;

namespace EventBus.Messages.Events;

/// <summary>
/// Published by Trip Service when rider is dropped off.
/// Triggers Payment Service to charge the rider and credit the driver.
/// </summary>
public record TripDropOffEvent : IntegrationBaseEvent
{
    public Guid TripId { get; init; }
    public Guid RiderId { get; init; }
    public Guid DriverId { get; init; }
    public decimal Amount { get; init; }
    public PaymentMethod PaymentMethod { get; init; } = default!; // "Wallet" | "Card"
    public string? GatewayToken { get; init; }
    public string Currency { get; init; } = "VND";
}
