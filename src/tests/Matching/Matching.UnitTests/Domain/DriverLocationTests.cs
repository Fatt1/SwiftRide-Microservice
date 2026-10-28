using Matching.Domain.Entities;
using Matching.UnitTests.Common.Builders;
using Xunit;

namespace Matching.UnitTests.Domain;

public class DriverLocationTests
{
    [Fact]
    public void Create_WithValidParameters_InitializesPropertiesCorrectly()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        const string fullName = "Tran Van B";
        const double lat = 10.762622;
        const double lng = 106.660172;

        // Act
        var driverLocation = DriverLocation.Create(driverId, fullName, lat, lng, isAvailable: true);

        // Assert
        Assert.NotNull(driverLocation);
        Assert.Equal(driverId, driverLocation.DriverId);
        Assert.Equal(fullName, driverLocation.FullName);
        Assert.True(driverLocation.IsAvailable);
        Assert.Equal(lat, driverLocation.Latitude);
        Assert.Equal(lng, driverLocation.Longitude);
    }

    [Fact]
    public void CreatePoint_WithLatAndLng_OrdersCoordinatesAsLongitudeFirst()
    {
        // Arrange
        const double lat = 10.8231;
        const double lng = 106.6297;

        // Act
        var point = DriverLocation.CreatePoint(lat, lng);

        // Assert
        Assert.NotNull(point);
        Assert.Equal(lng, point.Coordinates.Longitude);
        Assert.Equal(lat, point.Coordinates.Latitude);
    }

    [Fact]
    public void UpdateLocation_WithCoordinates_UpdatesCoordinatesAndTimestamp()
    {
        // Arrange
        var driverLocation = new DriverLocationBuilder()
            .WithLocation(10.7769, 106.7009)
            .Build();
        const double newLat = 10.8000;
        const double newLng = 106.7500;
        var beforeUpdate = DateTime.UtcNow;

        // Act
        driverLocation.UpdateLocation(newLat, newLng);

        // Assert
        Assert.Equal(newLat, driverLocation.Latitude);
        Assert.Equal(newLng, driverLocation.Longitude);
        Assert.True(driverLocation.UpdatedAt >= beforeUpdate);
    }

    [Fact]
    public void UpdateLocation_WithGeoJsonPoint_UpdatesLocationDirectly()
    {
        // Arrange
        var driverLocation = new DriverLocationBuilder().Build();
        var newPoint = DriverLocation.CreatePoint(10.8500, 106.7800);

        // Act
        driverLocation.UpdateLocation(newPoint);

        // Assert
        Assert.Equal(10.8500, driverLocation.Latitude);
        Assert.Equal(106.7800, driverLocation.Longitude);
    }

    [Fact]
    public void SetAvailability_WhenToggled_UpdatesAvailabilityAndTimestamp()
    {
        // Arrange
        var driverLocation = new DriverLocationBuilder()
            .WithAvailability(true)
            .Build();
        var beforeUpdate = DateTime.UtcNow;

        // Act
        driverLocation.SetAvailability(false);

        // Assert
        Assert.False(driverLocation.IsAvailable);
        Assert.True(driverLocation.UpdatedAt >= beforeUpdate);
    }
}
