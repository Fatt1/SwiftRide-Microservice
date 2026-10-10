using Contracts.Domain;
using Payment.Domain.Enums;
using Shared.Enums.Payments;
using Shared.Exceptions;

namespace Payment.Domain.Entities;

public class PaymentTransaction : EntityAuditableBase<Guid>
{
    public Guid TripId { get; private set; }
    public Guid RiderId { get; private set; }
    public Guid DriverId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public PaymentStatus Status { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public string? GatewayToken { get; private set; }
    public string? GatewayResponse { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    // Navigation
    public ICollection<LedgerEntry> LedgerEntries { get; private set; } = [];
    public ICollection<Refund> Refunds { get; private set; } = [];

    private PaymentTransaction() { }

    public static PaymentTransaction Create(
        Guid tripId,
        Guid riderId,
        Guid driverId,
        decimal amount,
        PaymentMethod paymentMethod,
        Guid idempotencyKey,
        string? gatewayToken = null,
        string currency = "VND")
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));
        if (tripId == Guid.Empty || riderId == Guid.Empty || driverId == Guid.Empty || idempotencyKey == Guid.Empty)
            throw new DomainException("Payment identifiers cannot be empty.");
        if (riderId == driverId || !Enum.IsDefined(paymentMethod) || currency != "VND")
            throw new DomainException("Invalid participants, payment method or currency.");

        return new PaymentTransaction
        {
            Id = Guid.CreateVersion7(),
            TripId = tripId,
            RiderId = riderId,
            DriverId = driverId,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            PaymentMethod = paymentMethod,
            IdempotencyKey = idempotencyKey,
            GatewayToken = gatewayToken,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkCompleted(string? gatewayResponse = null)
    {
        EnsureStatus(PaymentStatus.Pending);
        Status = PaymentStatus.Completed;
        GatewayResponse = gatewayResponse;
        ProcessedAt = DateTimeOffset.UtcNow;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason, string? gatewayResponse = null)
    {
        EnsureStatus(PaymentStatus.Pending);
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        GatewayResponse = gatewayResponse;
        ProcessedAt = DateTimeOffset.UtcNow;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRefunded()
    {
        EnsureStatus(PaymentStatus.Completed);
        Status = PaymentStatus.Refunded;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureStatus(PaymentStatus expected)
    {
        if (Status != expected)
            throw new DomainException($"Payment must be {expected}; current status is {Status}.");
    }
}
