using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Shared.Exceptions;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Domain;

public class RefundTests
{
    [Fact]
    public void Create_WithValidParameters_InitializesPendingRefund()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        const decimal amount = 85_000m;
        const string reason = "Trip cancelled before driver arrival";

        // Act
        var refund = Refund.Create(paymentId, amount, reason);

        // Assert
        refund.ShouldNotBeNull();
        refund.PaymentId.ShouldBe(paymentId);
        refund.Amount.ShouldBe(amount);
        refund.Reason.ShouldBe(reason);
        refund.Status.ShouldBe(RefundStatus.Pending);
        refund.ProcessedAt.ShouldBeNull();
        refund.CreatedAt.ShouldNotBe(default);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15_000)]
    public void Create_WithNonPositiveAmount_ThrowsDomainException(decimal invalidAmount)
    {
        // Arrange & Act & Assert
        Should.Throw<DomainException>(() =>
            Refund.Create(Guid.NewGuid(), invalidAmount, "Customer complaint"));
    }

    [Fact]
    public void Create_WithEmptyPaymentId_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        Should.Throw<DomainException>(() =>
            Refund.Create(Guid.Empty, 50_000m, "Customer complaint"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidReason_ThrowsDomainException(string? invalidReason)
    {
        // Arrange & Act & Assert
        Should.Throw<DomainException>(() =>
            Refund.Create(Guid.NewGuid(), 50_000m, invalidReason!));
    }

    [Fact]
    public void MarkCompleted_WhenPending_TransitionsToCompleted()
    {
        // Arrange
        var refund = Refund.Create(Guid.NewGuid(), 60_000m, "Valid reason");

        // Act
        refund.MarkCompleted();

        // Assert
        refund.Status.ShouldBe(RefundStatus.Completed);
        refund.ProcessedAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkCompleted_WhenAlreadyCompleted_ThrowsDomainException()
    {
        // Arrange
        var refund = Refund.Create(Guid.NewGuid(), 60_000m, "Valid reason");
        refund.MarkCompleted();

        // Act & Assert
        var ex = Should.Throw<DomainException>(() => refund.MarkCompleted());
        ex.Message.ShouldContain("Refund has already been processed");
    }

    [Fact]
    public void MarkFailed_WhenPending_TransitionsToFailed()
    {
        // Arrange
        var refund = Refund.Create(Guid.NewGuid(), 60_000m, "Valid reason");

        // Act
        refund.MarkFailed();

        // Assert
        refund.Status.ShouldBe(RefundStatus.Failed);
        refund.ProcessedAt.ShouldNotBeNull();
    }
}
