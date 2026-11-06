using NSubstitute;
using Payment.Application.Payments;
using Payment.Application.Payments.Contexts;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Repositories;
using Payment.Infrastructure.Strategies;
using Payment.UnitTests.Common.Builders;
using Shared.Enums.Payments;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Strategies;

public class WalletPaymentStrategyTests
{
    private readonly IWalletRepository _wallets = Substitute.For<IWalletRepository>();
    private readonly ILedgerRepository _ledger = Substitute.For<ILedgerRepository>();
    private readonly LedgerPostingService _ledgerPosting;
    private readonly WalletPaymentStrategy _strategy;

    public WalletPaymentStrategyTests()
    {
        _ledgerPosting = new LedgerPostingService(_ledger);
        _strategy = new WalletPaymentStrategy(_wallets, _ledgerPosting);
    }

    [Fact]
    public async Task ChargeAsync_WithValidWalletsAndSufficientBalance_ReturnsSuccessAndTransfersFunds()
    {
        // Arrange
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        const decimal fareAmount = 100_000m;

        var payment = new PaymentTransactionBuilder()
            .WithRiderId(riderId)
            .WithDriverId(driverId)
            .WithAmount(fareAmount)
            .WithPaymentMethod(PaymentMethod.Wallet)
            .Build();

        var riderWallet = new WalletBuilder().WithUserId(riderId).WithBalance(200_000m).Build();
        var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(50_000m).Build();

        _wallets.GetByUserIdAsync(riderId, Arg.Any<CancellationToken>()).Returns(riderWallet);
        _wallets.GetByUserIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driverWallet);

        var context = new PaymentExecutionContext(payment);

        // Act
        var result = await _strategy.ChargeAsync(context, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        riderWallet.Balance.ShouldBe(100_000m);
        driverWallet.Balance.ShouldBe(150_000m);
        _ledger.Received(2).Add(Arg.Any<LedgerEntry>());
    }

    [Fact]
    public async Task ChargeAsync_WhenRiderHasInsufficientBalance_ReturnsConflictError()
    {
        // Arrange
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        const decimal fareAmount = 150_000m;

        var payment = new PaymentTransactionBuilder()
            .WithRiderId(riderId)
            .WithDriverId(driverId)
            .WithAmount(fareAmount)
            .WithPaymentMethod(PaymentMethod.Wallet)
            .Build();

        var riderWallet = new WalletBuilder().WithUserId(riderId).WithBalance(50_000m).Build();
        var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(100_000m).Build();

        _wallets.GetByUserIdAsync(riderId, Arg.Any<CancellationToken>()).Returns(riderWallet);
        _wallets.GetByUserIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driverWallet);

        var context = new PaymentExecutionContext(payment);

        // Act
        var result = await _strategy.ChargeAsync(context, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Message.ShouldContain("Insufficient rider wallet balance");
        _ledger.DidNotReceive().Add(Arg.Any<LedgerEntry>());
    }

    [Fact]
    public async Task ChargeAsync_WhenRiderWalletNotFound_ReturnsConflictError()
    {
        // Arrange
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        var payment = new PaymentTransactionBuilder()
            .WithRiderId(riderId)
            .WithDriverId(driverId)
            .WithPaymentMethod(PaymentMethod.Wallet)
            .Build();

        _wallets.GetByUserIdAsync(riderId, Arg.Any<CancellationToken>()).Returns((Wallet?)null);
        _wallets.GetByUserIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(new WalletBuilder().WithUserId(driverId).Build());

        var context = new PaymentExecutionContext(payment);

        // Act
        var result = await _strategy.ChargeAsync(context, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Message.ShouldContain("A rider wallet with matching currency is required");
    }

    [Fact]
    public async Task ChargeAsync_WhenDriverWalletNotFound_ReturnsConflictError()
    {
        // Arrange
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        var payment = new PaymentTransactionBuilder()
            .WithRiderId(riderId)
            .WithDriverId(driverId)
            .WithPaymentMethod(PaymentMethod.Wallet)
            .Build();

        _wallets.GetByUserIdAsync(riderId, Arg.Any<CancellationToken>()).Returns(new WalletBuilder().WithUserId(riderId).Build());
        _wallets.GetByUserIdAsync(driverId, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

        var context = new PaymentExecutionContext(payment);

        // Act
        var result = await _strategy.ChargeAsync(context, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Message.ShouldContain("A driver wallet is required");
    }

    [Fact]
    public async Task RefundAsync_WithSufficientDriverBalance_ReturnsSuccessAndReversesFunds()
    {
        // Arrange
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        const decimal refundAmount = 70_000m;

        var payment = new PaymentTransactionBuilder()
            .WithRiderId(riderId)
            .WithDriverId(driverId)
            .WithAmount(refundAmount)
            .WithPaymentMethod(PaymentMethod.Wallet)
            .WithStatus(PaymentStatus.Completed)
            .Build();

        var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(100_000m).Build();
        var riderWallet = new WalletBuilder().WithUserId(riderId).WithBalance(20_000m).Build();

        _wallets.GetByUserIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driverWallet);
        _wallets.GetByUserIdAsync(riderId, Arg.Any<CancellationToken>()).Returns(riderWallet);

        var context = new RefundExecutionContext(payment);

        // Act
        var result = await _strategy.RefundAsync(context, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        driverWallet.Balance.ShouldBe(30_000m);
        riderWallet.Balance.ShouldBe(90_000m);
        _ledger.Received(2).Add(Arg.Any<LedgerEntry>());
    }

    [Fact]
    public async Task RefundAsync_WhenDriverHasInsufficientBalance_ReturnsConflictError()
    {
        // Arrange
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        const decimal refundAmount = 100_000m;

        var payment = new PaymentTransactionBuilder()
            .WithRiderId(riderId)
            .WithDriverId(driverId)
            .WithAmount(refundAmount)
            .WithPaymentMethod(PaymentMethod.Wallet)
            .WithStatus(PaymentStatus.Completed)
            .Build();

        var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(10_000m).Build();
        var riderWallet = new WalletBuilder().WithUserId(riderId).WithBalance(20_000m).Build();

        _wallets.GetByUserIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driverWallet);
        _wallets.GetByUserIdAsync(riderId, Arg.Any<CancellationToken>()).Returns(riderWallet);

        var context = new RefundExecutionContext(payment);

        // Act
        var result = await _strategy.RefundAsync(context, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Message.ShouldContain("insufficient balance");
    }
}
