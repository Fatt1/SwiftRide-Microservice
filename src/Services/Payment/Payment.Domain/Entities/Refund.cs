using Contracts.Domain;
using Payment.Domain.Enums;
using Shared.Exceptions;

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
            throw new DomainException("Refund amount must be greater than zero");
        if (paymentId == Guid.Empty || string.IsNullOrWhiteSpace(reason))
            throw new DomainException("A payment and refund reason are required.");

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
        EnsurePending();
        Status = RefundStatus.Completed;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed()
    {
        EnsurePending();
        Status = RefundStatus.Failed;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != RefundStatus.Pending)
            throw new DomainException("Refund has already been processed.");
    }
}
