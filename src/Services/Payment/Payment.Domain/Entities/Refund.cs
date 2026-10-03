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

    public static Refund Create(Guid paymentId, decimal amount, string reason)
    {
        if (amount <= 0)
            throw new ArgumentException("Refund amount must be greater than zero", nameof(amount));

        return new Refund
        {
            Id = Guid.CreateVersion7(),
            PaymentId = paymentId,
            Amount = amount,
            Reason = reason,
            Status = RefundStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkCompleted()
    {
        Status = RefundStatus.Completed;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed()
    {
        Status = RefundStatus.Failed;
        ProcessedAt = DateTimeOffset.UtcNow;
    }
}
