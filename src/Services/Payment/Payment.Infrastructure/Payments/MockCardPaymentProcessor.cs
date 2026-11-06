using System.Text.Json;
using Payment.Application.Abstractions;
using Payment.Application.Payments.Results;

namespace Payment.Infrastructure.Payments;

public sealed class MockCardPaymentProcessor : ICardPaymentProcessor
{
    public Task<CardPaymentResult> ChargeAsync(decimal amount, string token, Guid idempotencyKey, int retryAttempt, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // sucess
        // fail

        // 1. mock-fail: Lần đầu (retryAttempt == 0) ném lỗi mạng để MassTransit retry, lần sau sẽ thành công
        if (token == "mock-fail" && retryAttempt == 0)
            throw new TransientCardPaymentException("Mock gateway connection was lost.");
        if (token == "mock-fail" && retryAttempt == 1)
            throw new TransientCardPaymentException("Mock gateway connection was lost.");
        if (token == "mock-fail" && retryAttempt == 2)
            throw new TransientCardPaymentException("Mock gateway connection was lost.");
        if (token == "mock-fail" && retryAttempt == 3)
            return ProcessAsync("charge", amount, "mock-decline", idempotencyKey, ct);


        return ProcessAsync("charge", amount, token, idempotencyKey, ct);
    }

    public Task<CardPaymentResult> RefundAsync(decimal amount, string token, Guid paymentId, CancellationToken ct) =>
        ProcessAsync("refund", amount, token, paymentId, ct);

    private static Task<CardPaymentResult> ProcessAsync(string operation, decimal amount, string token, Guid key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return token switch
        {
            // 1. mock-success hoặc mock-fail (sau khi retry): Thành công
            "mock-success" or "mock-fail" => Task.FromResult(new CardPaymentResult(
                IsSuccess: amount > 0,
                Response: JsonSerializer.Serialize(new { operation, amount, reference = $"mock-{operation}-{key:N}", success = amount > 0 }),
                FailureReason: amount > 0 ? null : "Invalid amount.")),

            // 2. mock-decline: Từ chối thẻ
            "mock-decline" => Task.FromResult(new CardPaymentResult(
                IsSuccess: false,
                Response: JsonSerializer.Serialize(new { operation, amount, reference = $"mock-{operation}-{key:N}", success = false }),
                FailureReason: "Mock card was declined.")),

            // Mọi token khác: Không hỗ trợ
            _ => Task.FromResult(new CardPaymentResult(
                IsSuccess: false,
                Response: JsonSerializer.Serialize(new { operation, amount, reference = $"mock-{operation}-{key:N}", success = false }),
                FailureReason: "Unsupported mock card token."))
        };
    }
}
