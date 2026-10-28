using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.UnitTests.Common.Builders;
using Xunit;

namespace Matching.UnitTests.Domain;

public class MatchSessionTests
{
    [Fact]
    public void CreatePoint_WithLatAndLng_PlacesLongitudeFirstInGeoJson()
    {
        // Arrange
        const double lat = 10.7769;
        const double lng = 106.7009;

        // Act
        var point = MatchSession.CreatePoint(lat, lng);

        // Assert
        Assert.NotNull(point);
        Assert.Equal(lng, point.Coordinates.Longitude);
        Assert.Equal(lat, point.Coordinates.Latitude);
    }

    [Fact]
    public void SetPickupLocation_WhenCalled_UpdatesCoordinatesAndLastModified()
    {
        // Arrange
        var session = new MatchSessionBuilder()
            .WithPickupLocation(10.7000, 106.6000)
            .Build();
        const double newLat = 10.7800;
        const double newLng = 106.6900;
        var beforeUpdate = DateTime.UtcNow;

        // Act
        session.SetPickupLocation(newLat, newLng);

        // Assert
        Assert.Equal(newLat, session.PickupLat);
        Assert.Equal(newLng, session.PickupLng);
        Assert.True(session.LastModifiedAt >= beforeUpdate);
    }

    [Fact]
    public void AddAttempt_WhenDriverAttempted_AppendsDriverAttemptWithUtcTimestamp()
    {
        // Arrange
        var session = new MatchSessionBuilder().Build();
        var driverId = Guid.NewGuid();
        var beforeAttempt = DateTime.UtcNow;

        // Act
        session.AddAttempt(driverId);

        // Assert
        Assert.Single(session.DriverAttempts);
        Assert.Equal(driverId, session.DriverAttempts[0].DriverId);
        Assert.True(session.DriverAttempts[0].AttemptedAt >= beforeAttempt);
    }

    [Fact]
    public void MarkMatched_WhenDriverFound_SetsStatusToMatchedAndAssignsDriverId()
    {
        // Arrange
        var session = new MatchSessionBuilder()
            .WithStatus(MatchSessionStatus.Searching)
            .Build();
        var driverId = Guid.NewGuid();
        var beforeMatch = DateTime.UtcNow;

        // Act
        session.MarkMatched(driverId);

        // Assert
        Assert.Equal(MatchSessionStatus.Matched, session.Status);
        Assert.Equal(driverId, session.MatchedDriverId);
        Assert.True(session.LastModifiedAt >= beforeMatch);
    }

    [Fact]
    public void MarkNoDriver_WhenSearchExhausted_SetsStatusToNoDriver()
    {
        // Arrange
        var session = new MatchSessionBuilder()
            .WithStatus(MatchSessionStatus.Searching)
            .Build();
        var beforeUpdate = DateTime.UtcNow;

        // Act
        session.MarkNoDriver();

        // Assert
        Assert.Equal(MatchSessionStatus.NoDriver, session.Status);
        Assert.True(session.LastModifiedAt >= beforeUpdate);
    }

    [Fact]
    public void MarkExpired_WhenTimeoutReached_SetsStatusToExpired()
    {
        // Arrange
        var session = new MatchSessionBuilder()
            .WithStatus(MatchSessionStatus.Searching)
            .Build();
        var beforeUpdate = DateTime.UtcNow;

        // Act
        session.MarkExpired();

        // Assert
        Assert.Equal(MatchSessionStatus.Expired, session.Status);
        Assert.True(session.LastModifiedAt >= beforeUpdate);
    }
}
