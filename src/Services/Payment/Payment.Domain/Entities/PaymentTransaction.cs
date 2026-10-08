using Contracts.Domain;
using Payment.Domain.Enums;
using Shared.Enums.Payments;

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
    private readonly List<Refund> _refunds = [];
    public IReadOnlyCollection<Refund> Refunds => _refunds.AsReadOnly();

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
        PaymentRules.Id(tripId);
        PaymentRules.Id(riderId);
        PaymentRules.Id(driverId);
        PaymentRules.Id(idempotencyKey);
        PaymentRules.Amount(amount);
        PaymentRules.Currency(currency);
        PaymentRules.Require(riderId != driverId, "Rider and driver must be different.");
        PaymentRules.Require(Enum.IsDefined(paymentMethod), "Invalid payment method.");
        PaymentRules.Require(paymentMethod == PaymentMethod.Card
            ? !string.IsNullOrWhiteSpace(gatewayToken)
            : gatewayToken is null, "Card requires a token; Wallet must not include a token.");

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
        PaymentRules.Require(Status == PaymentStatus.Pending, "Only pending payments can complete.");
        Status = PaymentStatus.Completed;
        GatewayResponse = gatewayResponse;
        ProcessedAt = DateTimeOffset.UtcNow;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason, string? gatewayResponse = null)
    {
        PaymentRules.Require(Status == PaymentStatus.Pending, "Only pending payments can fail.");
        PaymentRules.Require(!string.IsNullOrWhiteSpace(reason) && reason.Length <= 500,
            "Failure reason is required and must not exceed 500 characters.");
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        GatewayResponse = gatewayResponse;
        ProcessedAt = DateTimeOffset.UtcNow;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRefunded()
    {
        PaymentRules.Require(Status == PaymentStatus.Completed, "Only completed payments can be refunded.");
        PaymentRules.Require(Refunds.Count(r => r.Status == RefundStatus.Completed) == 1,
            "A completed full refund is required.");
        Status = PaymentStatus.Refunded;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public Refund RequestRefund(string reason)
    {
        PaymentRules.Require(Status == PaymentStatus.Completed, "Only completed payments can be refunded.");
        PaymentRules.Require(!Refunds.Any(r => r.Status is RefundStatus.Pending or RefundStatus.Completed),
            "A refund is already pending or completed.");
        var refund = Refund.Create(this, reason);
        _refunds.Add(refund);
        return refund;
    }

}
