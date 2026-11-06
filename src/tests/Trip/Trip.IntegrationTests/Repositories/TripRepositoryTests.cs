using Microsoft.EntityFrameworkCore;
using Trip.Domain.Enums;
using Trip.Infrastructure.Persistence;
using Trip.Infrastructure.Repositories;
using Trip.IntegrationTests.Common.Builders;
using Trip.IntegrationTests.Fixtures;
using Xunit;

namespace Trip.IntegrationTests.Repositories;

public class TripRepositoryTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private (TripDbContext DbContext, TripRepository Repository) CreateRepository()
    {
        var db = fixture.GetDbContext();
        return (db, new TripRepository(db));
    }

    [Fact]
    public async Task CreateAsync_WithValidTrip_PersistsTripInDatabase()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        var trip = new TripBuilder()
            .WithEstimatedFare(65000m)
            .WithDistanceKm(6.2m)
            .Build();

        // Act
        await repo.CreateAsync(trip);
        await db.SaveChangesAsync();

        // Assert
        var persisted = await repo.FindByIdAsync(trip.Id);
        Assert.NotNull(persisted);
        Assert.Equal(trip.RiderId, persisted.RiderId);
        Assert.Equal(trip.PickupAddress, persisted.PickupAddress);
        Assert.Equal(trip.DropoffAddress, persisted.DropoffAddress);
        Assert.Equal(65000m, persisted.EstimatedFare);
        Assert.Equal(6.2m, persisted.DistanceKm);
        Assert.Equal(TripStatus.Requested, persisted.Status);
    }

    [Fact]
    public async Task FindByIdAsync_WhenTripExists_ReturnsMatchingTrip()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        var trip = new TripBuilder().Build();
        await repo.CreateAsync(trip);
        await db.SaveChangesAsync();

        // Act
        var result = await repo.FindByIdAsync(trip.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(trip.Id, result.Id);
        Assert.Equal(trip.RiderId, result.RiderId);
    }

    [Fact]
    public async Task Update_WhenStatusAndFareChange_UpdatesEntityInDatabase()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        var trip = new TripBuilder()
            .WithStatus(TripStatus.PickedUp)
            .Build();
        await repo.CreateAsync(trip);
        await db.SaveChangesAsync();

        // Act
        trip.MarkDroppedOff(72000m);
        await repo.Update(trip);
        await repo.SaveChangesAsync();

        // Assert
        var updated = await repo.FindByIdAsync(trip.Id);
        Assert.NotNull(updated);
        Assert.Equal(TripStatus.DroppedOff, updated.Status);
        Assert.Equal(72000m, updated.FinalFare);
    }

    [Fact]
    public async Task FindAll_ReturnsAllPersistedTrips()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        var trip1 = new TripBuilder().Build();
        var trip2 = new TripBuilder().Build();
        await repo.CreateAsync(trip1);
        await repo.CreateAsync(trip2);
        await db.SaveChangesAsync();

        // Act
        var trips = await repo.FindAll(trackChanges: false).ToListAsync();

        // Assert
        Assert.Equal(2, trips.Count);
        Assert.Contains(trips, t => t.Id == trip1.Id);
        Assert.Contains(trips, t => t.Id == trip2.Id);
    }
}
