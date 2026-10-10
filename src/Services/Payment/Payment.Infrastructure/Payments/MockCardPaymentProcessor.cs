using System.Text.Json;
using Payment.Application.Abstractions;
using Payment.Application.Payments.Results;

namespace Payment.Infrastructure.Payments;

public sealed class MockCardPaymentProcessor : ICardPaymentProcessor
{
    public Task<CardPaymentResult> ChargeAsync(decimal amount, string token, Guid idempotencyKey, CancellationToken ct) =>
        ProcessAsync("charge", amount, token, idempotencyKey, ct);
    public Task<CardPaymentResult> RefundAsync(decimal amount, string token, Guid paymentId, CancellationToken ct) =>
        ProcessAsync("refund", amount, token, paymentId, ct);

    private static Task<CardPaymentResult> ProcessAsync(string operation, decimal amount, string token, Guid key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var success = amount > 0 && token == "mock-success";
        var reason = success ? null : token == "mock-decline" ? "Mock card was declined." : "Unsupported mock card token.";
        var response = JsonSerializer.Serialize(new { operation, amount, reference = $"mock-{operation}-{key:N}", success });
        return Task.FromResult(new CardPaymentResult(success, response, reason));
    }
}
