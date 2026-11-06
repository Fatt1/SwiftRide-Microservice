using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Shared.Enums.Payments;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Domain;

public class LedgerEntryTests
{
    [Fact]
    public void Create_WithValidParameters_InitializesLedgerEntry()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        const decimal amount = 120_000m;
        const string description = "Trip payment charge";

        // Act
        var entry = LedgerEntry.Create(
            paymentId,
            walletId,
            PaymentMethod.Wallet,
            EntryType.Debit,
            amount,
            description);

        // Assert
        entry.ShouldNotBeNull();
        entry.PaymentId.ShouldBe(paymentId);
        entry.WalletId.ShouldBe(walletId);
        entry.PaymentMethod.ShouldBe(PaymentMethod.Wallet);
        entry.EntryType.ShouldBe(EntryType.Debit);
        entry.Amount.ShouldBe(amount);
        entry.Description.ShouldBe(description);
        entry.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public void Create_WithWalletMethodAndNullWalletId_ThrowsArgumentException()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() =>
            LedgerEntry.Create(
                paymentId,
                null,
                PaymentMethod.Wallet,
                EntryType.Debit,
                50_000m));

        ex.ParamName.ShouldBe("walletId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void Create_WithNonPositiveAmount_ThrowsArgumentException(decimal invalidAmount)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() =>
            LedgerEntry.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                PaymentMethod.Wallet,
                EntryType.Debit,
                invalidAmount));
    }

    [Fact]
    public void Create_WithCardMethodAndNullWalletId_Succeeds()
    {
        // Arrange & Act
        var entry = LedgerEntry.Create(
            Guid.NewGuid(),
            null,
            PaymentMethod.Card,
            EntryType.Debit,
            100_000m,
            "Card direct payment");

        // Assert
        entry.ShouldNotBeNull();
        entry.WalletId.ShouldBeNull();
        entry.PaymentMethod.ShouldBe(PaymentMethod.Card);
    }

    [Fact]
    public void CreateTopUp_WithValidParameters_InitializesTopUpCreditEntry()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        const decimal amount = 500_000m;

        // Act
        var entry = LedgerEntry.CreateTopUp(requestId, paymentId, walletId, amount);

        // Assert
        entry.ShouldNotBeNull();
        entry.Id.ShouldBe(requestId);
        entry.PaymentId.ShouldBe(paymentId);
        entry.WalletId.ShouldBe(walletId);
        entry.Amount.ShouldBe(amount);
        entry.EntryType.ShouldBe(EntryType.Credit);
        entry.PaymentMethod.ShouldBe(PaymentMethod.Wallet);
        entry.Description.ShouldBe("Wallet top-up");
    }

    [Fact]
    public void CreateTopUp_WithEmptyWalletId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() =>
            LedgerEntry.CreateTopUp(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 100_000m));
    }
}
