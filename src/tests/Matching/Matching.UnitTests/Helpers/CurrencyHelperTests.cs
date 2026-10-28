using Matching.Application.Helpers;
using Xunit;

namespace Matching.UnitTests.Helpers;

public class CurrencyHelperTests
{
    [Theory]
    [InlineData(67891.0, 68000.0)]
    [InlineData(67200.0, 67000.0)]
    [InlineData(67500.0, 68000.0)]
    [InlineData(50000.0, 50000.0)]
    public void RoundVnd_WithStandardAmount_RoundsToNearestThousand(double amount, double expected)
    {
        // Arrange & Act
        var result = amount.RoundVnd();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-15000.0)]
    public void RoundVnd_WithZeroOrNegativeAmount_ReturnsZero(double amount)
    {
        // Arrange & Act
        var result = amount.RoundVnd();

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void RoundVnd_WithInvalidStep_ReturnsZero()
    {
        // Arrange
        const double amount = 50000.0;

        // Act
        var result = amount.RoundVnd(roundStep: 0);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void RoundVnd_WithCustomStep_RoundsAccurately()
    {
        // Arrange
        const double amount = 12345.0;
        const double customStep = 500.0;

        // Act
        var result = amount.RoundVnd(customStep);

        // Assert
        Assert.Equal(12500.0, result);
    }
}
