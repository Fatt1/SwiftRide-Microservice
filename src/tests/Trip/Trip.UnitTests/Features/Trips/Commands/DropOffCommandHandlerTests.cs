using EventBus.Messages.Events;
using MassTransit;
using NSubstitute;
using Shared;
using Shared.Enums.Payments;
using Trip.Application.Features.Trips.Commands.DropOff;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
using Trip.UnitTests.Common.Builders;
using Xunit;

namespace Trip.UnitTests.Features.Trips.Commands;

public class DropOffCommandHandlerTests
{
    private readonly ITripRepository _tripRepository = Substitute.For<ITripRepository>();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();

    private DropOffHandler CreateHandler() => new(_tripRepository, _publishEndpoint);

    [Fact]
    public async Task Handle_WhenPaymentMethodIsInvalid_ReturnsBadError()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new DropOffCommand(Guid.NewGuid(), (PaymentMethod)999);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<BadError>(result.Error);
        Assert.Equal("Payment method is invalid.", result.Error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenCardPaymentWithoutGatewayToken_ReturnsBadError(string? gatewayToken)
    {
        // Arrange
        var handler = CreateHandler();
        var command = new DropOffCommand(Guid.NewGuid(), PaymentMethod.Card, gatewayToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<BadError>(result.Error);
        Assert.Contains("GatewayToken is required", result.Error.Message);
    }

    [Fact]
    public async Task Handle_WhenCardPaymentGatewayTokenExceedsMaxLength_ReturnsBadError()
    {
        // Arrange
        var handler = CreateHandler();
        var longToken = new string('a', 129);
        var command = new DropOffCommand(Guid.NewGuid(), PaymentMethod.Card, longToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<BadError>(result.Error);
        Assert.Contains("maximum 128 characters", result.Error.Message);
    }

    [Fact]
    public async Task Handle_WhenTripNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _tripRepository.FindByIdAsync(tripId).Returns((TripEntity?)null);

        var handler = CreateHandler();
        var command = new DropOffCommand(tripId, PaymentMethod.Wallet);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Theory]
    [InlineData(TripStatus.Requested)]
    [InlineData(TripStatus.DriverAccepted)]
    [InlineData(TripStatus.DroppedOff)]
    [InlineData(TripStatus.Completed)]
    [InlineData(TripStatus.Cancelled)]
    public async Task Handle_WhenTripNotPickedUp_ReturnsConflictError(TripStatus invalidStatus)
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(invalidStatus).Build();
        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var handler = CreateHandler();
        var command = new DropOffCommand(trip.Id, PaymentMethod.Wallet);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ConflictError>(result.Error);
        Assert.Contains(invalidStatus.ToString(), result.Error.Message);
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<TripDropOffEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTripIsPickedUp_MarksDroppedOffPublishesTripDropOffEventAndReturnsSuccess()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        const decimal estimatedFare = 65000m;
        var trip = new TripBuilder()
            .WithDriverId(driverId)
            .WithEstimatedFare(estimatedFare)
            .WithStatus(TripStatus.PickedUp)
            .Build();

        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var handler = CreateHandler();
        var command = new DropOffCommand(trip.Id, PaymentMethod.Wallet);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.DroppedOff, trip.Status);
        Assert.Equal(estimatedFare, trip.FinalFare);

        await _tripRepository.Received(1).Update(trip);
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<TripDropOffEvent>(e =>
                e.TripId == trip.Id &&
                e.RiderId == trip.RiderId &&
                e.DriverId == driverId &&
                e.Amount == estimatedFare &&
                e.PaymentMethod == PaymentMethod.Wallet &&
                e.Currency == "VND"),
            Arg.Any<CancellationToken>());
        await _tripRepository.Received(1).SaveChangesAsync();
    }
}
