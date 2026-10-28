using Matching.Domain.Entities;
using Matching.Infrastructure.Repositories;
using Matching.IntegrationTests.Common.Builders;
using Matching.IntegrationTests.Fixtures;
using Xunit;

namespace Matching.IntegrationTests.Repositories;

public class DriverLocationRepositoryTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private DriverLocationRepository CreateRepository() => new(fixture.GetDatabase());

    [Fact]
    public async Task GetNearestAvailableDriverAsync_WithinRadius_ReturnsNearestDriver()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var repo = CreateRepository();

        const double pickupLat = 10.7725;
        const double pickupLon = 106.6980;

        // Driver 1: Very close (~100m)
        var closeDriver = new DriverLocationBuilder()
            .WithDriverId(Guid.NewGuid())
            .WithFullName("Close Driver")
            .WithLocation(pickupLat + 0.0008, pickupLon + 0.0008)
            .WithAvailability(true)
            .Build();

        // Driver 2: Farther away (~3km)
        var farDriver = new DriverLocationBuilder()
            .WithDriverId(Guid.NewGuid())
            .WithFullName("Far Driver")
            .WithLocation(pickupLat + 0.025, pickupLon + 0.025)
            .WithAvailability(true)
            .Build();

        await repo.CreateAsync(closeDriver);
        await repo.CreateAsync(farDriver);

        // Act
        var result = await repo.GetNearestAvailableDriverAsync(
            latitude: pickupLat,
            longitude: pickupLon,
            maxRadiusInMeters: 5000,
            excludedDriverIds: []);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(closeDriver.DriverId, result.DriverId);
        Assert.Equal("Close Driver", result.FullName);
    }

    [Fact]
    public async Task GetNearestAvailableDriverAsync_WhenNearestIsExcluded_ReturnsSecondNearestDriver()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var repo = CreateRepository();

        const double pickupLat = 10.7725;
        const double pickupLon = 106.6980;

        var driver1 = new DriverLocationBuilder()
            .WithDriverId(Guid.NewGuid())
            .WithFullName("Driver 1")
            .WithLocation(pickupLat + 0.001, pickupLon + 0.001)
            .WithAvailability(true)
            .Build();

        var driver2 = new DriverLocationBuilder()
            .WithDriverId(Guid.NewGuid())
            .WithFullName("Driver 2")
            .WithLocation(pickupLat + 0.005, pickupLon + 0.005)
            .WithAvailability(true)
            .Build();

        await repo.CreateAsync(driver1);
        await repo.CreateAsync(driver2);

        // Act: Exclude driver 1
        var result = await repo.GetNearestAvailableDriverAsync(
            latitude: pickupLat,
            longitude: pickupLon,
            maxRadiusInMeters: 5000,
            excludedDriverIds: [driver1.DriverId]);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(driver2.DriverId, result.DriverId);
        Assert.Equal("Driver 2", result.FullName);
    }

    [Fact]
    public async Task GetNearestAvailableDriverAsync_WhenDriverUnavailable_DoesNotReturnDriver()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var repo = CreateRepository();

        const double pickupLat = 10.7725;
        const double pickupLon = 106.6980;

        var unavailableDriver = new DriverLocationBuilder()
            .WithLocation(pickupLat + 0.001, pickupLon + 0.001)
            .WithAvailability(false) // unavailable
            .Build();

        await repo.CreateAsync(unavailableDriver);

        // Act
        var result = await repo.GetNearestAvailableDriverAsync(
            latitude: pickupLat,
            longitude: pickupLon,
            maxRadiusInMeters: 5000,
            excludedDriverIds: []);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetNearestAvailableDriverAsync_BeyondMaxRadius_ReturnsNull()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var repo = CreateRepository();

        const double pickupLat = 10.7725;
        const double pickupLon = 106.6980;

        // Driver located ~15km away
        var distantDriver = new DriverLocationBuilder()
            .WithLocation(pickupLat + 0.15, pickupLon + 0.15)
            .WithAvailability(true)
            .Build();

        await repo.CreateAsync(distantDriver);

        // Act: max radius 2000 meters (2km)
        var result = await repo.GetNearestAvailableDriverAsync(
            latitude: pickupLat,
            longitude: pickupLon,
            maxRadiusInMeters: 2000,
            excludedDriverIds: []);

        // Assert
        Assert.Null(result);
    }
}
