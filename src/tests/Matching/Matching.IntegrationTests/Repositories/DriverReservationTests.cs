using Matching.Domain.Entities;
using Matching.Infrastructure.Repositories;
using Matching.IntegrationTests.Fixtures;
using Xunit;
namespace Matching.IntegrationTests.Repositories;
public class DriverReservationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Reserve_CompetingTrips_OnlyOneWinsAndStaleReleaseCannotFreeWinner()
    {
        // Arrange
        await fixture.ResetDatabaseAsync(); var repo = new DriverLocationRepository(fixture.GetDatabase());
        var driver = DriverLocation.Create(Guid.NewGuid(), "Driver", 10.7, 106.7); await repo.CreateAsync(driver);
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        // Act
        var results = await Task.WhenAll(repo.TryReserveAsync(driver.DriverId, a), repo.TryReserveAsync(driver.DriverId, b));
        var winner = results[0] ? a : b; var loser = results[0] ? b : a;
        await repo.ReleaseAsync(driver.DriverId, loser);
        // Assert
        Assert.Single(results.Where(x => x)); var persisted = await repo.GetByDriverIdAsync(driver.DriverId);
        Assert.False(persisted!.IsAvailable); Assert.Equal(winner, persisted.ActiveTripId);
        // Act
        await repo.ReleaseAsync(driver.DriverId, winner);
        // Assert
        Assert.True((await repo.GetByDriverIdAsync(driver.DriverId))!.IsAvailable);
    }
}
