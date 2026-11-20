using Payment.Application.Abstractions;
using Payment.Infrastructure.Payments;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Strategies;

public class MockCardPaymentProcessorTests
{
    private readonly MockCardPaymentProcessor _processor = new();

    [Fact]
    public async Task ChargeAsync_WithRetrySuccess_SucceedsOnSecondRetry()
    {
        const string token = "mock-retry-success";
        var key = Guid.NewGuid();

        await Should.ThrowAsync<TransientCardPaymentException>(() =>
            _processor.ChargeAsync(100_000m, token, key, 0, CancellationToken.None));
        await Should.ThrowAsync<TransientCardPaymentException>(() =>
            _processor.ChargeAsync(100_000m, token, key, 1, CancellationToken.None));

        var result = await _processor.ChargeAsync(100_000m, token, key, 2, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Response.ShouldContain($"mock-charge-{key:N}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public async Task ChargeAsync_WithRetryExhausted_ThrowsOnEveryAttempt(int retryAttempt)
    {
        await Should.ThrowAsync<TransientCardPaymentException>(() =>
            _processor.ChargeAsync(100_000m, "mock-retry-exhausted", Guid.NewGuid(), retryAttempt, CancellationToken.None));
    }

    [Fact]
    public async Task ChargeAsync_WithDecline_ReturnsConfirmedFailure()
    {
        var result = await _processor.ChargeAsync(100_000m, "mock-decline", Guid.NewGuid(), 0, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.FailureReason.ShouldBe("Mock card was declined.");
    }
}
