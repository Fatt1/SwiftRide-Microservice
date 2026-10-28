using EventBus.Messages.Events;
using MassTransit;
using NSubstitute;
using Shared;
using Trip.Application.Features.Trips.Commands.DriverAccept;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
using Trip.UnitTests.Common.Builders;
using Xunit;

namespace Trip.UnitTests.Features.Trips.Commands;

public class DriverAcceptCommandHandlerTests
{
    private readonly ITripRepository _tripRepository = Substitute.For<ITripRepository>();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private DriverAcceptHandler CreateHandler() => new(_tripRepository, _publishEndpoint);

    [Fact]
    public async Task Handle_WhenTripNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _tripRepository.FindByIdAsync(tripId).Returns((TripEntity?)null);

        var handler = CreateHandler();
        var command = new DriverAcceptCommand(tripId, Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<NotFoundError>(result.Error);
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<TripAcceptedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTripNotRequested_ReturnsConflictError()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.PickedUp).Build();
        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var handler = CreateHandler();
        var command = new DriverAcceptCommand(trip.Id, Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ConflictError>(result.Error);
        Assert.Contains("PickedUp", result.Error.Message);
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<TripAcceptedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTripIsRequested_AcceptsTripPublishesEventAndReturnsSuccess()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.Requested).Build();
        var driverId = Guid.NewGuid();
        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var handler = CreateHandler();
        var command = new DriverAcceptCommand(trip.Id, driverId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.DriverAccepted, trip.Status);
        Assert.Equal(driverId, trip.DriverId);

        await _tripRepository.Received(1).Update(trip);
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<TripAcceptedEvent>(e =>
                e.TripId == trip.Id &&
                e.DriverId == driverId &&
                e.RiderId == trip.RiderId),
            Arg.Any<CancellationToken>());
        await _tripRepository.Received(1).SaveChangesAsync();
    }
}
