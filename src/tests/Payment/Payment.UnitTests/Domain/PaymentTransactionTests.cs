using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.UnitTests.Common.Builders;
using Shared.Enums.Payments;
using Shared.Exceptions;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Domain;

public class PaymentTransactionTests
{
    [Fact]
    public void Create_WithValidParameters_InitializesPaymentInPendingStatus()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        const decimal amount = 75_000m;

        // Act
        var payment = PaymentTransaction.Create(
            tripId,
            riderId,
            driverId,
            amount,
            PaymentMethod.Wallet,
            idempotencyKey);

        // Assert
        payment.ShouldNotBeNull();
        payment.TripId.ShouldBe(tripId);
        payment.RiderId.ShouldBe(riderId);
        payment.DriverId.ShouldBe(driverId);
        payment.Amount.ShouldBe(amount);
        payment.Currency.ShouldBe("VND");
        payment.Status.ShouldBe(PaymentStatus.Pending);
        payment.PaymentMethod.ShouldBe(PaymentMethod.Wallet);
        payment.IdempotencyKey.ShouldBe(idempotencyKey);
        payment.Type.ShouldBe(PaymentType.TripPayment);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void Create_WithNonPositiveAmount_ThrowsArgumentException(decimal invalidAmount)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() =>
            PaymentTransaction.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                invalidAmount,
                PaymentMethod.Wallet,
                Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithEmptyTripId_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        Should.Throw<DomainException>(() =>
            PaymentTransaction.Create(
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid(),
                50_000m,
                PaymentMethod.Wallet,
                Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithIdenticalRiderAndDriver_ThrowsDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        var exception = Should.Throw<DomainException>(() =>
            PaymentTransaction.Create(
                Guid.NewGuid(),
                userId,
                userId,
                50_000m,
                PaymentMethod.Wallet,
                Guid.NewGuid()));

        exception.Message.ShouldContain("Invalid participants");
    }

    [Fact]
    public void Create_WithNonVndCurrency_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        Should.Throw<DomainException>(() =>
            PaymentTransaction.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                50_000m,
                PaymentMethod.Wallet,
                Guid.NewGuid(),
                currency: "USD"));
    }

    [Fact]
    public void CreateTopUp_WithValidParameters_ReturnsCompletedTopUpPayment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        const decimal amount = 200_000m;

        // Act
        var topUp = PaymentTransaction.CreateTopUp(userId, requestId, amount);

        // Assert
        topUp.ShouldNotBeNull();
        topUp.Type.ShouldBe(PaymentType.WalletTopUp);
        topUp.RiderId.ShouldBe(userId);
        topUp.Amount.ShouldBe(amount);
        topUp.IdempotencyKey.ShouldBe(requestId);
        topUp.PaymentMethod.ShouldBe(PaymentMethod.Wallet);
        topUp.Status.ShouldBe(PaymentStatus.Completed);
        topUp.ProcessedAt.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50000)]
    public void CreateTopUp_WithNonPositiveAmount_ThrowsDomainException(decimal invalidAmount)
    {
        // Arrange & Act & Assert
        Should.Throw<DomainException>(() =>
            PaymentTransaction.CreateTopUp(Guid.NewGuid(), Guid.NewGuid(), invalidAmount));
    }

    [Fact]
    public void MarkCompleted_WhenPending_TransitionsToCompleted()
    {
        // Arrange
        var payment = new PaymentTransactionBuilder()
            .WithStatus(PaymentStatus.Pending)
            .Build();
        const string gatewayResponse = "txn_123456";

        // Act
        payment.MarkCompleted(gatewayResponse);

        // Assert
        payment.Status.ShouldBe(PaymentStatus.Completed);
        payment.GatewayResponse.ShouldBe(gatewayResponse);
        payment.ProcessedAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkCompleted_WhenAlreadyCompleted_ThrowsDomainException()
    {
        // Arrange
        var payment = new PaymentTransactionBuilder()
            .WithStatus(PaymentStatus.Completed)
            .Build();

        // Act & Assert
        var ex = Should.Throw<DomainException>(() => payment.MarkCompleted());
        ex.Message.ShouldContain("Payment must be Pending");
    }

    [Fact]
    public void MarkFailed_WhenPending_TransitionsToFailedWithReason()
    {
        // Arrange
        var payment = new PaymentTransactionBuilder()
            .WithStatus(PaymentStatus.Pending)
            .Build();
        const string reason = "Card declined by issuing bank";

        // Act
        payment.MarkFailed(reason, "err_code_402");

        // Assert
        payment.Status.ShouldBe(PaymentStatus.Failed);
        payment.FailureReason.ShouldBe(reason);
        payment.GatewayResponse.ShouldBe("err_code_402");
        payment.ProcessedAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkRefunded_WhenCompleted_TransitionsToRefunded()
    {
        // Arrange
        var payment = new PaymentTransactionBuilder()
            .WithStatus(PaymentStatus.Completed)
            .Build();

        // Act
        payment.MarkRefunded();

        // Assert
        payment.Status.ShouldBe(PaymentStatus.Refunded);
    }

    [Fact]
    public void MarkRefunded_WhenPending_ThrowsDomainException()
    {
        // Arrange
        var payment = new PaymentTransactionBuilder()
            .WithStatus(PaymentStatus.Pending)
            .Build();

        // Act & Assert
        var ex = Should.Throw<DomainException>(() => payment.MarkRefunded());
        ex.Message.ShouldContain("Payment must be Completed");
    }
}
