using EventBus.Messages.Common;
using Shared.Enums.Payments;

namespace EventBus.Messages.Events;

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
    public PaymentMethod PaymentMethod { get; init; } = default!;
}
