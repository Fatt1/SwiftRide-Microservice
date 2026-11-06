using NSubstitute;
using Shared;
using Trip.Application.Features.Trips.Commands.CancelTrip;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
using Trip.UnitTests.Common.Builders;
using Xunit;

namespace Trip.UnitTests.Features.Trips.Commands;

public class CancelTripCommandHandlerTests
{
    private readonly ITripRepository _tripRepository = Substitute.For<ITripRepository>();

    private CancelTripHandler CreateHandler() => new(_tripRepository);

    [Fact]
    public async Task Handle_WhenTripNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _tripRepository.FindByIdAsync(tripId).Returns((TripEntity?)null);

        var handler = CreateHandler();
        var command = new CancelTripCommand(tripId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Theory]
    [InlineData(TripStatus.PickedUp)]
    [InlineData(TripStatus.DroppedOff)]
    [InlineData(TripStatus.Completed)]
    [InlineData(TripStatus.Cancelled)]
    public async Task Handle_WhenTripInNonCancellableStatus_ReturnsConflictError(TripStatus nonCancellableStatus)
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(nonCancellableStatus).Build();
        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var handler = CreateHandler();
        var command = new CancelTripCommand(trip.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ConflictError>(result.Error);
        Assert.Contains(nonCancellableStatus.ToString(), result.Error.Message);
        await _tripRepository.DidNotReceive().SaveChangesAsync();
    }

    [Theory]
    [InlineData(TripStatus.Requested)]
    [InlineData(TripStatus.DriverAccepted)]
    [InlineData(TripStatus.DriverRejected)]
    public async Task Handle_WhenTripInCancellableStatus_CancelsTripAndReturnsSuccess(TripStatus cancellableStatus)
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(cancellableStatus).Build();
        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var handler = CreateHandler();
        var command = new CancelTripCommand(trip.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.Cancelled, trip.Status);
        await _tripRepository.Received(1).Update(trip);
        await _tripRepository.Received(1).SaveChangesAsync();
    }
}
