using System.Text.Json;
using Payment.Application.Abstractions;
using Payment.Application.Payments.Results;

namespace Payment.Infrastructure.Payments;

public sealed class MockCardPaymentProcessor : ICardPaymentProcessor
{
    public Task<CardPaymentResult> ChargeAsync(decimal amount, string token, Guid idempotencyKey, int retryAttempt, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // MassTransit retries the same event once; no in-memory counter is needed.
        if (token == "mock-fail" && retryAttempt == 0)
            throw new TransientCardPaymentException("Mock gateway connection was lost.");
        return ProcessAsync("charge", amount, token, idempotencyKey, ct);
    }
    public Task<CardPaymentResult> RefundAsync(decimal amount, string token, Guid paymentId, CancellationToken ct) =>
        ProcessAsync("refund", amount, token, paymentId, ct);

    private static Task<CardPaymentResult> ProcessAsync(string operation, decimal amount, string token, Guid key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var success = amount > 0 && (token == "mock-success" || token == "mock-fail");
        var reason = success ? null : token == "mock-decline" ? "Mock card was declined." : "Unsupported mock card token.";
        var response = JsonSerializer.Serialize(new { operation, amount, reference = $"mock-{operation}-{key:N}", success });
        return Task.FromResult(new CardPaymentResult(success, response, reason));
    }
}
