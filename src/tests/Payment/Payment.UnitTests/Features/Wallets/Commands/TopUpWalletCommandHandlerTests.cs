using Microsoft.Extensions.Logging;
using NSubstitute;
using Payment.Application.Abstractions;
using Payment.Application.Features.Wallets.Commands.TopUpWallet;
using Payment.Application.Payments;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Repositories;
using Payment.UnitTests.Common.Builders;
using Shared;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Features.Wallets.Commands;

public class TopUpWalletCommandHandlerTests
{
    private readonly IWalletRepository _wallets = Substitute.For<IWalletRepository>();
    private readonly ILedgerRepository _ledger = Substitute.For<ILedgerRepository>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IPaymentTransaction _transactions = Substitute.For<IPaymentTransaction>();
    private readonly LedgerPostingService _ledgerPosting;
    private readonly ILogger<TopUpWalletCommandHandler> _logger = Substitute.For<ILogger<TopUpWalletCommandHandler>>();
    private readonly TopUpWalletCommandHandler _handler;

    public TopUpWalletCommandHandlerTests()
    {
        _transactions.BeginOwnedTransactionAsync().Returns(Substitute.For<IAsyncDisposable>());
        _ledgerPosting = new LedgerPostingService(_ledger);

        _handler = new TopUpWalletCommandHandler(
            _wallets,
            _ledger,
            _payments,
            _transactions,
            _ledgerPosting,
            _logger);
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _wallets.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

        var command = new TopUpWalletCommand(userId, Guid.NewGuid(), 100_000m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Handle_WhenIdempotentRequestMatchesExisting_ReturnsExistingWalletWithoutDoubleCrediting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        const decimal amount = 200_000m;

        var wallet = new WalletBuilder()
            .WithUserId(userId)
            .WithBalance(500_000m)
            .Build();

        var existingPayment = PaymentTransaction.CreateTopUp(userId, idempotencyKey, amount);
        var existingLedger = LedgerEntry.CreateTopUp(idempotencyKey, existingPayment.Id, wallet.Id, amount);

        _wallets.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);
        _payments.GetByIdempotencyKeyAsync(idempotencyKey, Arg.Any<CancellationToken>()).Returns(existingPayment);
        _ledger.GetByIdAsync(idempotencyKey, Arg.Any<CancellationToken>()).Returns(existingLedger);

        var command = new TopUpWalletCommand(userId, idempotencyKey, amount);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Balance.ShouldBe(500_000m); // Balance remains unchanged (no double credit)

        _payments.DidNotReceive().Add(Arg.Any<PaymentTransaction>());
        _ledger.DidNotReceive().Add(Arg.Any<LedgerEntry>());
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyConflictsWithDifferentPayment_ReturnsConflictError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        var wallet = new WalletBuilder().WithUserId(userId).Build();

        // Existing payment with different amount
        var existingPayment = PaymentTransaction.CreateTopUp(userId, idempotencyKey, 300_000m);

        _wallets.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);
        _payments.GetByIdempotencyKeyAsync(idempotencyKey, Arg.Any<CancellationToken>()).Returns(existingPayment);

        var command = new TopUpWalletCommand(userId, idempotencyKey, 100_000m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ConflictError>();
    }

    [Fact]
    public async Task Handle_WhenValid_CreditsWalletAndAddsPaymentAndLedger()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        const decimal initialBalance = 100_000m;
        const decimal topUpAmount = 250_000m;

        var wallet = new WalletBuilder()
            .WithUserId(userId)
            .WithBalance(initialBalance)
            .Build();

        _wallets.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);
        _payments.GetByIdempotencyKeyAsync(idempotencyKey, Arg.Any<CancellationToken>()).Returns((PaymentTransaction?)null);
        _ledger.GetByIdAsync(idempotencyKey, Arg.Any<CancellationToken>()).Returns((LedgerEntry?)null);

        var command = new TopUpWalletCommand(userId, idempotencyKey, topUpAmount);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        wallet.Balance.ShouldBe(initialBalance + topUpAmount);
        result.Value.Balance.ShouldBe(initialBalance + topUpAmount);

        _payments.Received(1).Add(Arg.Is<PaymentTransaction>(p =>
            p.Type == PaymentType.WalletTopUp &&
            p.Amount == topUpAmount &&
            p.Status == PaymentStatus.Completed));

        _ledger.Received(1).Add(Arg.Is<LedgerEntry>(l =>
            l.Amount == topUpAmount &&
            l.EntryType == EntryType.Credit &&
            l.WalletId == wallet.Id));

        await _transactions.Received(1).EndTransactionAsync();
    }
}
