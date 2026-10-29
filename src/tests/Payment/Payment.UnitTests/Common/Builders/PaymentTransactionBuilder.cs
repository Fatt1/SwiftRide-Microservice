using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Shared.Enums.Payments;

namespace Payment.UnitTests.Common.Builders;

public class PaymentTransactionBuilder
{
    private Guid _tripId = Guid.NewGuid();
    private Guid _riderId = Guid.NewGuid();
    private Guid _driverId = Guid.NewGuid();
    private decimal _amount = 100_000m;
    private PaymentMethod _paymentMethod = PaymentMethod.Wallet;
    private Guid _idempotencyKey = Guid.NewGuid();
    private string? _gatewayToken;
    private readonly string _currency = "VND";
    private PaymentStatus _status = PaymentStatus.Pending;
    private string? _failureReason;

    public PaymentTransactionBuilder WithTripId(Guid tripId)
    {
        _tripId = tripId;
        return this;
    }

    public PaymentTransactionBuilder WithRiderId(Guid riderId)
    {
        _riderId = riderId;
        return this;
    }

    public PaymentTransactionBuilder WithDriverId(Guid driverId)
    {
        _driverId = driverId;
        return this;
    }

    public PaymentTransactionBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public PaymentTransactionBuilder WithPaymentMethod(PaymentMethod method)
    {
        _paymentMethod = method;
        return this;
    }

    public PaymentTransactionBuilder WithIdempotencyKey(Guid idempotencyKey)
    {
        _idempotencyKey = idempotencyKey;
        return this;
    }

    public PaymentTransactionBuilder WithGatewayToken(string? token)
    {
        _gatewayToken = token;
        return this;
    }

    public PaymentTransactionBuilder WithStatus(PaymentStatus status)
    {
        _status = status;
        return this;
    }

    public PaymentTransactionBuilder WithFailureReason(string reason)
    {
        _failureReason = reason;
        return this;
    }

    public PaymentTransaction Build()
    {
        var payment = PaymentTransaction.Create(
            _tripId,
            _riderId,
            _driverId,
            _amount,
            _paymentMethod,
            _idempotencyKey,
            _gatewayToken,
            _currency);

        switch (_status)
        {
            case PaymentStatus.Pending:
                break;
            case PaymentStatus.Completed:
                payment.MarkCompleted("tx_success");
                break;
            case PaymentStatus.Failed:
                payment.MarkFailed(_failureReason ?? "Insufficient balance", "tx_failed");
                break;
            case PaymentStatus.Refunded:
                payment.MarkCompleted("tx_success");
                payment.MarkRefunded();
                break;
        }

        return payment;
    }
}
