using Contracts.Domain;
using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

public class Refund : EntityBase<Guid>
{
    public Guid PaymentId { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = default!;
    public RefundStatus Status { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation
    public PaymentTransaction Payment { get; private set; } = default!;

    private Refund() { }

    internal static Refund Create(PaymentTransaction payment, string reason)
    {
        PaymentRules.Require(!string.IsNullOrWhiteSpace(reason), "Refund reason is required.");

        return new Refund
        {
            Id = Guid.CreateVersion7(),
            PaymentId = payment.Id,
            Payment = payment,
            Amount = payment.Amount,
            Reason = reason,
            Status = RefundStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkCompleted()
    {
        PaymentRules.Require(Status == RefundStatus.Pending, "Only pending refunds can complete.");
        PaymentRules.Require(Payment.Status == PaymentStatus.Completed, "Payment must be completed.");
        Status = RefundStatus.Completed;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed()
    {
        PaymentRules.Require(Status == RefundStatus.Pending, "Only pending refunds can fail.");
        Status = RefundStatus.Failed;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

}
