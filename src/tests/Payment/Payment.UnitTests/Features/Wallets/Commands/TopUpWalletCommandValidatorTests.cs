using Payment.Application.Features.Wallets.Commands.TopUpWallet;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Features.Wallets.Commands;

public class TopUpWalletCommandValidatorTests
{
    private readonly TopUpWalletCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var command = new TopUpWalletCommand(Guid.NewGuid(), Guid.NewGuid(), 100_000m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ReturnsFailure()
    {
        // Arrange
        var command = new TopUpWalletCommand(Guid.Empty, Guid.NewGuid(), 100_000m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(TopUpWalletCommand.UserId));
    }

    [Fact]
    public void Validate_WithEmptyIdempotencyKey_ReturnsFailure()
    {
        // Arrange
        var command = new TopUpWalletCommand(Guid.NewGuid(), Guid.Empty, 100_000m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(TopUpWalletCommand.IdempotencyKey));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50_000)]
    public void Validate_WithNonPositiveAmount_ReturnsFailure(decimal invalidAmount)
    {
        // Arrange
        var command = new TopUpWalletCommand(Guid.NewGuid(), Guid.NewGuid(), invalidAmount);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(TopUpWalletCommand.Amount));
    }
}
