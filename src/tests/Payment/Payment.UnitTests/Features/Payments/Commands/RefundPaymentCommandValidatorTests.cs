using Payment.Application.Features.Payments.Commands.RefundPayment;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Features.Payments.Commands;

public class RefundPaymentCommandValidatorTests
{
    private readonly RefundPaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var command = new RefundPaymentCommand
        {
            PaymentId = Guid.NewGuid(),
            Reason = "Driver did not show up"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyPaymentId_ReturnsFailure()
    {
        // Arrange
        var command = new RefundPaymentCommand
        {
            PaymentId = Guid.Empty,
            Reason = "Driver did not show up"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RefundPaymentCommand.PaymentId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyReason_ReturnsFailure(string? invalidReason)
    {
        // Arrange
        var command = new RefundPaymentCommand
        {
            PaymentId = Guid.NewGuid(),
            Reason = invalidReason!
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RefundPaymentCommand.Reason));
    }

    [Fact]
    public void Validate_WithReasonExceedingMaxLength_ReturnsFailure()
    {
        // Arrange
        var command = new RefundPaymentCommand
        {
            PaymentId = Guid.NewGuid(),
            Reason = new string('A', 501)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RefundPaymentCommand.Reason));
    }
}
