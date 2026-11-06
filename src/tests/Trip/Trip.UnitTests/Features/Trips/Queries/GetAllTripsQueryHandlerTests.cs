using Microsoft.EntityFrameworkCore;
using Trip.Application.Features.Trips.Queries.GetAllTrips;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
using Trip.Infrastructure.Persistence;
using Trip.Infrastructure.Repositories;
using Trip.UnitTests.Common.Builders;
using Xunit;

namespace Trip.UnitTests.Features.Trips.Queries;

public class GetAllTripsQueryHandlerTests
{
    private (TripDbContext DbContext, ITripRepository Repository) CreateRepository()
    {
        var options = new DbContextOptionsBuilder<TripDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new TripDbContext(options);
        var repository = new TripRepository(context);
        return (context, repository);
    }

    [Fact]
    public async Task Handle_WhenTripsExist_ReturnsAllTripsOrderedByCreatedAtDesc()
    {
        // Arrange
        var (context, repository) = CreateRepository();
        var trip1 = new TripBuilder().Build();
        var trip2 = new TripBuilder().Build();
        await context.Trips.AddRangeAsync(trip1, trip2);
        await context.SaveChangesAsync();

        var handler = new GetAllTripsHandler(repository);
        var query = new GetAllTripsQuery(null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Handle_WithRiderIdFilter_ReturnsOnlyTripsForRider()
    {
        // Arrange
        var (context, repository) = CreateRepository();
        var riderId = Guid.NewGuid();
        var trip1 = new TripBuilder().WithRiderId(riderId).Build();
        var trip2 = new TripBuilder().WithRiderId(Guid.NewGuid()).Build();
        await context.Trips.AddRangeAsync(trip1, trip2);
        await context.SaveChangesAsync();

        var handler = new GetAllTripsHandler(repository);
        var query = new GetAllTripsQuery(riderId, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(riderId, result.Value[0].RiderId);
    }

    [Fact]
    public async Task Handle_WithDriverIdFilter_ReturnsOnlyTripsForDriver()
    {
        // Arrange
        var (context, repository) = CreateRepository();
        var driverId = Guid.NewGuid();
        var trip1 = new TripBuilder().WithDriverId(driverId).WithStatus(TripStatus.DriverAccepted).Build();
        var trip2 = new TripBuilder().WithDriverId(Guid.NewGuid()).WithStatus(TripStatus.DriverAccepted).Build();
        await context.Trips.AddRangeAsync(trip1, trip2);
        await context.SaveChangesAsync();

        var handler = new GetAllTripsHandler(repository);
        var query = new GetAllTripsQuery(null, driverId, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(driverId, result.Value[0].DriverId);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsOnlyTripsWithMatchingStatus()
    {
        // Arrange
        var (context, repository) = CreateRepository();
        var trip1 = new TripBuilder().WithStatus(TripStatus.Completed).Build();
        var trip2 = new TripBuilder().WithStatus(TripStatus.Requested).Build();
        await context.Trips.AddRangeAsync(trip1, trip2);
        await context.SaveChangesAsync();

        var handler = new GetAllTripsHandler(repository);
        var query = new GetAllTripsQuery(null, null, "Completed");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("Completed", result.Value[0].Status);
    }

    [Fact]
    public async Task Handle_WhenNoTripsMatch_ReturnsEmptyList()
    {
        // Arrange
        var (_, repository) = CreateRepository();
        var handler = new GetAllTripsHandler(repository);
        var query = new GetAllTripsQuery(Guid.NewGuid(), null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
