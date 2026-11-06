using NSubstitute;
using Shared;
using Trip.Application.Features.Trips.Commands.PickUp;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
using Trip.UnitTests.Common.Builders;
using Xunit;

namespace Trip.UnitTests.Features.Trips.Commands;

public class PickUpCommandHandlerTests
{
    private readonly ITripRepository _tripRepository = Substitute.For<ITripRepository>();

    private PickUpHandler CreateHandler() => new(_tripRepository);

    [Fact]
    public async Task Handle_WhenTripNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _tripRepository.FindByIdAsync(tripId).Returns((TripEntity?)null);

        var handler = CreateHandler();
        var command = new PickUpCommand(tripId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Theory]
    [InlineData(TripStatus.Requested)]
    [InlineData(TripStatus.PickedUp)]
    [InlineData(TripStatus.DroppedOff)]
    [InlineData(TripStatus.Completed)]
    [InlineData(TripStatus.Cancelled)]
    public async Task Handle_WhenTripNotDriverAccepted_ReturnsConflictError(TripStatus invalidStatus)
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(invalidStatus).Build();
        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var handler = CreateHandler();
        var command = new PickUpCommand(trip.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ConflictError>(result.Error);
        Assert.Contains(invalidStatus.ToString(), result.Error.Message);
        await _tripRepository.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WhenTripIsDriverAccepted_MarksPickedUpAndReturnsSuccess()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.DriverAccepted).Build();
        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var handler = CreateHandler();
        var command = new PickUpCommand(trip.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.PickedUp, trip.Status);
        await _tripRepository.Received(1).Update(trip);
        await _tripRepository.Received(1).SaveChangesAsync();
    }
}
