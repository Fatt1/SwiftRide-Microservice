using Payment.Application.Features.Payments.Commands.CreatePayment;
using Shared.Enums.Payments;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Features.Payments.Commands;

public class CreatePaymentCommandValidatorTests
{
    private readonly CreatePaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidWalletPayment_ReturnsSuccess()
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            TripId = Guid.NewGuid(),
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            Amount = 100_000m,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = Guid.NewGuid(),
            Currency = "VND"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyTripId_ReturnsFailure()
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            TripId = Guid.Empty,
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            Amount = 100_000m,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreatePaymentCommand.TripId));
    }

    [Fact]
    public void Validate_WhenRiderIdEqualsDriverId_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreatePaymentCommand
        {
            TripId = Guid.NewGuid(),
            RiderId = userId,
            DriverId = userId,
            Amount = 50_000m,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreatePaymentCommand.DriverId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void Validate_WithNonPositiveAmount_ReturnsFailure(decimal invalidAmount)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            TripId = Guid.NewGuid(),
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            Amount = invalidAmount,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreatePaymentCommand.Amount));
    }

    [Fact]
    public void Validate_WithInvalidCurrency_ReturnsFailure()
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            TripId = Guid.NewGuid(),
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            Amount = 50_000m,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = Guid.NewGuid(),
            Currency = "USD"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreatePaymentCommand.Currency));
    }

    [Fact]
    public void Validate_WithCardPaymentAndEmptyGatewayToken_ReturnsFailure()
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            TripId = Guid.NewGuid(),
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            Amount = 50_000m,
            PaymentMethod = PaymentMethod.Card,
            IdempotencyKey = Guid.NewGuid(),
            GatewayToken = null
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreatePaymentCommand.GatewayToken));
    }

    [Fact]
    public void Validate_WithCardPaymentAndValidGatewayToken_ReturnsSuccess()
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            TripId = Guid.NewGuid(),
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            Amount = 50_000m,
            PaymentMethod = PaymentMethod.Card,
            IdempotencyKey = Guid.NewGuid(),
            GatewayToken = "tok_visa_valid"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}
