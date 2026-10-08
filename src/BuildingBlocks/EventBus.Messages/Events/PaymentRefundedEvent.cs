using EventBus.Messages.Common;

namespace EventBus.Messages.Events;

public record PaymentRefundedEvent : IntegrationBaseEvent
{
    public Guid PaymentId { get; init; }
    public Guid RefundId { get; init; }
    public Guid TripId { get; init; }
    public decimal Amount { get; init; }
    public bool Succeeded { get; init; }
}

public record RefundPaymentRequestedEvent : IntegrationBaseEvent
{
    public Guid PaymentId { get; init; }
    public Guid IdempotencyKey { get; init; }
    public string Reason { get; init; } = "Saga compensation";
}
