using NSubstitute;
using Payment.Application.Payments;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Repositories;
using Payment.UnitTests.Common.Builders;
using Shared.Enums.Payments;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Payments;

public class LedgerPostingServiceTests
{
    private readonly ILedgerRepository _ledger = Substitute.For<ILedgerRepository>();
    private readonly LedgerPostingService _service;

    public LedgerPostingServiceTests()
    {
        _service = new LedgerPostingService(_ledger);
    }

    [Fact]
    public void Record_WithDebit_DebitsWalletAndAddsLedgerEntry()
    {
        // Arrange
        var wallet = new WalletBuilder().WithBalance(300_000m).Build();
        var paymentId = Guid.NewGuid();
        const decimal amount = 50_000m;

        // Act
        _service.Record(paymentId, wallet, PaymentMethod.Wallet, EntryType.Debit, amount, "Trip fare charge");

        // Assert
        wallet.Balance.ShouldBe(250_000m);
        _ledger.Received(1).Add(Arg.Is<LedgerEntry>(entry =>
            entry.PaymentId == paymentId &&
            entry.WalletId == wallet.Id &&
            entry.Amount == amount &&
            entry.EntryType == EntryType.Debit &&
            entry.PaymentMethod == PaymentMethod.Wallet));
    }

    [Fact]
    public void Record_WithCredit_CreditsWalletAndAddsLedgerEntry()
    {
        // Arrange
        var wallet = new WalletBuilder().WithBalance(100_000m).Build();
        var paymentId = Guid.NewGuid();
        const decimal amount = 80_000m;

        // Act
        _service.Record(paymentId, wallet, PaymentMethod.Wallet, EntryType.Credit, amount, "Driver earnings payout");

        // Assert
        wallet.Balance.ShouldBe(180_000m);
        _ledger.Received(1).Add(Arg.Is<LedgerEntry>(entry =>
            entry.PaymentId == paymentId &&
            entry.WalletId == wallet.Id &&
            entry.Amount == amount &&
            entry.EntryType == EntryType.Credit &&
            entry.PaymentMethod == PaymentMethod.Wallet));
    }

    [Fact]
    public void Record_WithoutWallet_AddsLedgerEntryWithoutWalletOperation()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        const decimal amount = 150_000m;

        // Act
        _service.Record(paymentId, null, PaymentMethod.Card, EntryType.Debit, amount, "External card charge");

        // Assert
        _ledger.Received(1).Add(Arg.Is<LedgerEntry>(entry =>
            entry.PaymentId == paymentId &&
            entry.WalletId == null &&
            entry.Amount == amount &&
            entry.EntryType == EntryType.Debit &&
            entry.PaymentMethod == PaymentMethod.Card));
    }

    [Fact]
    public void RecordTopUp_CreditsWalletAndAddsTopUpEntry()
    {
        // Arrange
        var wallet = new WalletBuilder().WithBalance(50_000m).Build();
        var requestId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        const decimal topUpAmount = 200_000m;

        // Act
        _service.RecordTopUp(requestId, paymentId, wallet, topUpAmount);

        // Assert
        wallet.Balance.ShouldBe(250_000m);
        _ledger.Received(1).Add(Arg.Is<LedgerEntry>(entry =>
            entry.Id == requestId &&
            entry.PaymentId == paymentId &&
            entry.WalletId == wallet.Id &&
            entry.Amount == topUpAmount &&
            entry.EntryType == EntryType.Credit));
    }
}
