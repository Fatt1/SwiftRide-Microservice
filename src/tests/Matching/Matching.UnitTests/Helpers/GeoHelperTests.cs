using Matching.Application.Helpers;
using Xunit;

namespace Matching.UnitTests.Helpers;

public class GeoHelperTests
{
    [Fact]
    public void CalculateDistanceInKm_WithSameCoordinates_ReturnsZero()
    {
        // Arrange
        const double lat = 10.7769;
        const double lon = 106.7009;

        // Act
        var distance = GeoHelper.CalculateDistanceInKm(lat, lon, lat, lon);

        // Assert
        Assert.Equal(0.0, distance);
    }

    [Fact]
    public void CalculateDistanceInKm_WithKnownCoordinates_ReturnsPositiveDistance()
    {
        // Arrange
        // Ben Thanh Market (10.7725, 106.6980) to Landmark 81 (10.7950, 106.7218) ~ 3.5 km
        const double benThanhLat = 10.7725;
        const double benThanhLon = 106.6980;
        const double landmarkLat = 10.7950;
        const double landmarkLon = 106.7218;

        // Act
        var distance = GeoHelper.CalculateDistanceInKm(benThanhLat, benThanhLon, landmarkLat, landmarkLon);

        // Assert
        Assert.InRange(distance, 3.0, 4.5);
    }

    [Fact]
    public void CalculateDistanceInMeters_ReturnsKilometersTimesThousand()
    {
        // Arrange
        const double originLat = 10.7725;
        const double originLon = 106.6980;
        const double destLat = 10.7950;
        const double destLon = 106.7218;

        // Act
        var distanceKm = GeoHelper.CalculateDistanceInKm(originLat, originLon, destLat, destLon);
        var distanceMeters = GeoHelper.CalculateDistanceInMeters(originLat, originLon, destLat, destLon);

        // Assert
        Assert.Equal(distanceKm * 1000.0, distanceMeters, precision: 3);
    }

    [Fact]
    public void CalculateEstimatedTime_WithZeroDistance_ReturnsZeroTimeSpan()
    {
        // Arrange
        const double distanceKm = 0.0;

        // Act
        var time = GeoHelper.CalculateEstimatedTime(distanceKm);

        // Assert
        Assert.Equal(TimeSpan.Zero, time);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void CalculateEstimatedTime_WithInvalidSpeed_ThrowsArgumentOutOfRangeException(double invalidSpeed)
    {
        // Arrange
        const double distanceKm = 10.0;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GeoHelper.CalculateEstimatedTime(distanceKm, invalidSpeed));
    }

    [Fact]
    public void CalculateEstimatedMinutes_WithZeroDistance_ReturnsZero()
    {
        // Arrange
        const double distanceKm = 0.0;

        // Act
        var minutes = GeoHelper.CalculateEstimatedMinutes(distanceKm);

        // Assert
        Assert.Equal(0, minutes);
    }

    [Fact]
    public void CalculateEstimatedMinutes_WithSmallPositiveDistance_ReturnsAtLeastOneMinute()
    {
        // Arrange
        const double smallDistanceKm = 0.05; // 50m

        // Act
        var minutes = GeoHelper.CalculateEstimatedMinutes(smallDistanceKm);

        // Assert
        Assert.True(minutes >= 1);
    }

    [Fact]
    public void CalculateTravelEstimate_ReturnsAccurateTuple()
    {
        // Arrange
        const double originLat = 10.7725;
        const double originLon = 106.6980;
        const double destLat = 10.7950;
        const double destLon = 106.7218;

        // Act
        var (distanceKm, estimatedMinutes, duration) = GeoHelper.CalculateTravelEstimate(originLat, originLon, destLat, destLon);

        // Assert
        Assert.True(distanceKm > 0);
        Assert.True(estimatedMinutes > 0);
        Assert.True(duration > TimeSpan.Zero);
    }
}
