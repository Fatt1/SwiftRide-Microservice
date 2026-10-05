using EventBus.Messages.Common;

namespace EventBus.Messages.Events;

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
