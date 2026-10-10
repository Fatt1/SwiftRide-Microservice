using Payment.Application.Payments.Results;

namespace Payment.Application.Abstractions;

public interface ICardPaymentProcessor
{
    Task<CardPaymentResult> ChargeAsync(decimal amount, string token, Guid idempotencyKey, CancellationToken ct);
    Task<CardPaymentResult> RefundAsync(decimal amount, string token, Guid paymentId, CancellationToken ct);
}
