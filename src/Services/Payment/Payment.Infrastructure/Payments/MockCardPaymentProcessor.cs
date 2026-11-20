using System.Text.Json;
using Payment.Application.Abstractions;
using Payment.Application.Payments.Results;

namespace Payment.Infrastructure.Payments;

public sealed class MockCardPaymentProcessor : ICardPaymentProcessor
{
    public Task<CardPaymentResult> ChargeAsync(decimal amount, string token, Guid idempotencyKey, int retryAttempt, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (token == "mock-retry-exhausted" ||
            (retryAttempt < 2 && token == "mock-retry-success"))
        {
            throw new TransientCardPaymentException("Mock gateway connection was lost.");
        }

        return ProcessAsync("charge", amount, token, idempotencyKey, ct);
    }

    public Task<CardPaymentResult> RefundAsync(decimal amount, string token, Guid paymentId, CancellationToken ct) =>
        ProcessAsync("refund", amount, token, paymentId, ct);

    private static Task<CardPaymentResult> ProcessAsync(string operation, decimal amount, string token, Guid key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return token switch
        {
            "mock-success" or "mock-retry-success" => Task.FromResult(new CardPaymentResult(
                IsSuccess: amount > 0,
                Response: JsonSerializer.Serialize(new { operation, amount, reference = $"mock-{operation}-{key:N}", success = amount > 0 }),
                FailureReason: amount > 0 ? null : "Invalid amount.")),

            "mock-decline" => Task.FromResult(new CardPaymentResult(
                IsSuccess: false,
                Response: JsonSerializer.Serialize(new { operation, amount, reference = $"mock-{operation}-{key:N}", success = false }),
                FailureReason: "Mock card was declined.")),

            _ => Task.FromResult(new CardPaymentResult(
                IsSuccess: false,
                Response: JsonSerializer.Serialize(new { operation, amount, reference = $"mock-{operation}-{key:N}", success = false }),
                FailureReason: "Unsupported mock card token."))
        };
    }
}
