using Payment.Domain.Entities;
using Payment.UnitTests.Common.Builders;
using Shared.Exceptions;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Domain;

public class WalletTests
{
    [Fact]
    public void Create_WithValidParameters_InitializesProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const decimal initialBalance = 150_000m;

        // Act
        var wallet = Wallet.Create(userId, initialBalance);

        // Assert
        wallet.ShouldNotBeNull();
        wallet.UserId.ShouldBe(userId);
        wallet.Balance.ShouldBe(initialBalance);
        wallet.Currency.ShouldBe("VND");
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        Should.Throw<DomainException>(() => Wallet.Create(Guid.Empty, 100_000m));
    }

    [Fact]
    public void Create_WithNegativeInitialBalance_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        Should.Throw<DomainException>(() => Wallet.Create(Guid.NewGuid(), -10_000m));
    }

    [Fact]
    public void Create_WithUnsupportedCurrency_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        Should.Throw<DomainException>(() => Wallet.Create(Guid.NewGuid(), 0, "USD"));
    }

    [Fact]
    public void Credit_WithPositiveAmount_IncreasesBalance()
    {
        // Arrange
        var wallet = new WalletBuilder().WithBalance(100_000m).Build();

        // Act
        wallet.Credit(50_000m);

        // Assert
        wallet.Balance.ShouldBe(150_000m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10_000)]
    public void Credit_WithNonPositiveAmount_ThrowsArgumentException(decimal invalidAmount)
    {
        // Arrange
        var wallet = new WalletBuilder().Build();

        // Act & Assert
        Should.Throw<ArgumentException>(() => wallet.Credit(invalidAmount));
    }

    [Fact]
    public void Debit_WithSufficientBalance_DecreasesBalance()
    {
        // Arrange
        var wallet = new WalletBuilder().WithBalance(200_000m).Build();

        // Act
        wallet.Debit(80_000m);

        // Assert
        wallet.Balance.ShouldBe(120_000m);
    }

    [Fact]
    public void Debit_WithInsufficientBalance_ThrowsDomainException()
    {
        // Arrange
        var wallet = new WalletBuilder().WithBalance(50_000m).Build();

        // Act & Assert
        var ex = Should.Throw<DomainException>(() => wallet.Debit(100_000m));
        ex.Message.ShouldContain("Insufficient balance in wallet");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-20_000)]
    public void Debit_WithNonPositiveAmount_ThrowsDomainException(decimal invalidAmount)
    {
        // Arrange
        var wallet = new WalletBuilder().Build();

        // Act & Assert
        Should.Throw<DomainException>(() => wallet.Debit(invalidAmount));
    }
}
