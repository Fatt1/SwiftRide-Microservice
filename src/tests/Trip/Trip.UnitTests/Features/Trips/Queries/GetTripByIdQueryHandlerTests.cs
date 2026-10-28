using NSubstitute;
using Shared;
using Trip.Application.Features.Trips.Queries.GetTripById;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
using Trip.UnitTests.Common.Builders;
using Xunit;

namespace Trip.UnitTests.Features.Trips.Queries;

public class GetTripByIdQueryHandlerTests
{
    private readonly ITripRepository _tripRepository = Substitute.For<ITripRepository>();

    private GetTripByIdHandler CreateHandler() => new(_tripRepository);

    [Fact]
    public async Task Handle_WhenTripNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _tripRepository.FindByIdAsync(tripId).Returns((TripEntity?)null);

        var handler = CreateHandler();
        var query = new GetTripByIdQuery(tripId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task Handle_WhenTripExists_ReturnsTripDto()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var trip = new TripBuilder()
            .WithDriverId(driverId)
            .WithEstimatedFare(70000m)
            .WithDistanceKm(7.5m)
            .WithStatus(TripStatus.DriverAccepted)
            .Build();

        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var handler = CreateHandler();
        var query = new GetTripByIdQuery(trip.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(trip.Id, result.Value.Id);
        Assert.Equal(trip.RiderId, result.Value.RiderId);
        Assert.Equal(driverId, result.Value.DriverId);
        Assert.Equal(TripStatus.DriverAccepted.ToString(), result.Value.Status);
        Assert.Equal(trip.PickupAddress, result.Value.PickupAddress);
        Assert.Equal(trip.DropoffAddress, result.Value.DropoffAddress);
        Assert.Equal(70000m, result.Value.EstimatedFare);
        Assert.Equal(7.5m, result.Value.DistanceKm);
    }
}
