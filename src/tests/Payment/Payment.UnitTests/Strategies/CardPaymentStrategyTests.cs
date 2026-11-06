using NSubstitute;
using Payment.Application.Abstractions;
using Payment.Application.Payments;
using Payment.Application.Payments.Contexts;
using Payment.Application.Payments.Results;
using Payment.Domain.Entities;
using Payment.Domain.Repositories;
using Payment.Infrastructure.Strategies;
using Payment.UnitTests.Common.Builders;
using Shared.Enums.Payments;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Strategies;

public class CardPaymentStrategyTests
{
    private readonly IWalletRepository _wallets = Substitute.For<IWalletRepository>();
    private readonly ICardPaymentProcessor _processor = Substitute.For<ICardPaymentProcessor>();
    private readonly ILedgerRepository _ledger = Substitute.For<ILedgerRepository>();
    private readonly LedgerPostingService _ledgerPosting;
    private readonly CardPaymentStrategy _strategy;

    public CardPaymentStrategyTests()
    {
        _ledgerPosting = new LedgerPostingService(_ledger);
        _strategy = new CardPaymentStrategy(_wallets, _processor, _ledgerPosting);
    }

    [Fact]
    public async Task ChargeAsync_WhenDriverWalletNotFound_ReturnsConflictError()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var payment = new PaymentTransactionBuilder()
            .WithDriverId(driverId)
            .WithPaymentMethod(PaymentMethod.Card)
            .WithGatewayToken("tok_123")
            .Build();

        _wallets.GetByUserIdAsync(driverId, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

        var context = new PaymentExecutionContext(payment);

        // Act
        var result = await _strategy.ChargeAsync(context, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Message.ShouldContain("A driver wallet is required");
    }

    [Fact]
    public async Task ChargeAsync_WhenProcessorSucceeds_RecordsLedgerAndReturnsSuccess()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        const decimal amount = 120_000m;
        var payment = new PaymentTransactionBuilder()
            .WithDriverId(driverId)
            .WithAmount(amount)
            .WithPaymentMethod(PaymentMethod.Card)
            .WithGatewayToken("tok_123")
            .Build();

        var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(50_000m).Build();
        _wallets.GetByUserIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driverWallet);

        _processor.ChargeAsync(amount, "tok_123", payment.IdempotencyKey, 0, Arg.Any<CancellationToken>())
            .Returns(new CardPaymentResult(true, "card_tx_success"));

        var context = new PaymentExecutionContext(payment);

        // Act
        var result = await _strategy.ChargeAsync(context, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        context.ProcessorResponse.ShouldBe("card_tx_success");
        driverWallet.Balance.ShouldBe(170_000m); // Driver earnings credited
        _ledger.Received(2).Add(Arg.Any<LedgerEntry>());
    }

    [Fact]
    public async Task ChargeAsync_WhenProcessorFails_ReturnsConflictError()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        const decimal amount = 120_000m;
        var payment = new PaymentTransactionBuilder()
            .WithDriverId(driverId)
            .WithAmount(amount)
            .WithPaymentMethod(PaymentMethod.Card)
            .WithGatewayToken("tok_invalid")
            .Build();

        var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(50_000m).Build();
        _wallets.GetByUserIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driverWallet);

        _processor.ChargeAsync(amount, "tok_invalid", payment.IdempotencyKey, 0, Arg.Any<CancellationToken>())
            .Returns(new CardPaymentResult(false, "card_tx_failed", "Card expired"));

        var context = new PaymentExecutionContext(payment);

        // Act
        var result = await _strategy.ChargeAsync(context, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Message.ShouldBe("Card expired");
        _ledger.DidNotReceive().Add(Arg.Any<LedgerEntry>());
    }

    [Fact]
    public async Task RefundAsync_WhenDriverHasInsufficientBalance_ReturnsConflictError()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        const decimal refundAmount = 100_000m;

        var payment = new PaymentTransactionBuilder()
            .WithDriverId(driverId)
            .WithAmount(refundAmount)
            .WithPaymentMethod(PaymentMethod.Card)
            .WithGatewayToken("tok_123")
            .Build();

        var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(10_000m).Build();
        _wallets.GetByUserIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driverWallet);

        var context = new RefundExecutionContext(payment);

        // Act
        var result = await _strategy.RefundAsync(context, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Message.ShouldContain("insufficient balance");
        await _processor.DidNotReceive().RefundAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefundAsync_WhenProcessorSucceeds_DebitsDriverAndReturnsSuccess()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        const decimal refundAmount = 100_000m;

        var payment = new PaymentTransactionBuilder()
            .WithDriverId(driverId)
            .WithAmount(refundAmount)
            .WithPaymentMethod(PaymentMethod.Card)
            .WithGatewayToken("tok_123")
            .Build();

        var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(200_000m).Build();
        _wallets.GetByUserIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(driverWallet);

        _processor.RefundAsync(refundAmount, "tok_123", payment.Id, Arg.Any<CancellationToken>())
            .Returns(new CardPaymentResult(true, "refund_tx_success"));

        var context = new RefundExecutionContext(payment);

        // Act
        var result = await _strategy.RefundAsync(context, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        driverWallet.Balance.ShouldBe(100_000m);
        _ledger.Received(2).Add(Arg.Any<LedgerEntry>());
    }
}
