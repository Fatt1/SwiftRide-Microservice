using EventBus.Messages.Events;
using MassTransit;
using NSubstitute;
using Shared;
using Trip.Application.Features.Trips.Commands.DriverReject;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
using Trip.UnitTests.Common.Builders;
using Xunit;

namespace Trip.UnitTests.Features.Trips.Commands;

public class DriverRejectCommandHandlerTests
{
    private readonly ITripRepository _tripRepository = Substitute.For<ITripRepository>();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private DriverRejectHandler CreateHandler() => new(_tripRepository, _publishEndpoint);

    [Fact]
    public async Task Handle_WhenTripNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _tripRepository.FindByIdAsync(tripId).Returns((TripEntity?)null);

        var handler = CreateHandler();
        var command = new DriverRejectCommand(tripId, Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<NotFoundError>(result.Error);
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<DriverRejectEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTripNotRequested_ReturnsConflictError()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.Completed).Build();
        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var handler = CreateHandler();
        var command = new DriverRejectCommand(trip.Id, Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ConflictError>(result.Error);
        Assert.Contains("Completed", result.Error.Message);
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<DriverRejectEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTripIsRequested_PublishesDriverRejectEventAndReturnsSuccess()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.Requested).Build();
        var driverId = Guid.NewGuid();
        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var handler = CreateHandler();
        var command = new DriverRejectCommand(trip.Id, driverId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<DriverRejectEvent>(e =>
                e.TripId == trip.Id &&
                e.DriverId == driverId &&
                e.RiderId == trip.RiderId &&
                e.Reason == "Tài xế đã từ chối nhận chuyến xe này."),
            Arg.Any<CancellationToken>());
        await _tripRepository.Received(1).SaveChangesAsync();
    }
}
