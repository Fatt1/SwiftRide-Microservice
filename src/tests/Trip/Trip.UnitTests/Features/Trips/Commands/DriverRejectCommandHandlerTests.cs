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

    private MediatR.IRequestHandler<DriverRejectCommand, Shared.Result<DriverRejectResponse>> CreateHandler() => global::Trip.UnitTests.ValidatedHandlers.DriverReject(_tripRepository, _publishEndpoint);

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

        trip.SetDriverOffer(trip.MatchSessionId!, driverId, 1);
        var command = new DriverRejectCommand(trip.Id, driverId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.RejectCount);
        Assert.Equal(1, result.Value.MaxAllowedRejections);
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<DriverRejectEvent>(e =>
                e.TripId == trip.Id &&
                e.DriverId == driverId &&
                e.RiderId == trip.RiderId &&
                e.RejectCount == 1 && !e.TripFailed),
            Arg.Any<CancellationToken>());
        await _tripRepository.Received(1).SaveChangesAsync();
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_ReplayedRejection_DoesNotIncrementCountOrReplaceCurrentOffer(bool latest)
    {
        // Arrange
        var trip = new TripBuilder().Build();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        trip.SetDriverOffer(trip.MatchSessionId!, a, 3); trip.RejectByDriver(a);
        trip.SetDriverOffer(trip.MatchSessionId!, b, 3); trip.RejectByDriver(b);
        trip.SetDriverOffer(trip.MatchSessionId!, c, 3);
        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);
        var command = new DriverRejectCommand(trip.Id, latest ? b : a);
        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);
        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ConflictError>(result.Error);
        Assert.Equal(2, trip.RejectCount);
        Assert.Equal(c, trip.OfferedDriverId);
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<DriverRejectEvent>(), Arg.Any<CancellationToken>());
    }


    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Handle_RejectionExceedsK_ReturnsLimitAndPublishesTripFailed(int k)
    {
        // Arrange
        var trip = new TripBuilder().Build();
        for (var i = 0; i < k; i++)
        {
            var previous = Guid.NewGuid();
            trip.SetDriverOffer(trip.MatchSessionId!, previous, k);
            trip.RejectByDriver(previous);
        }
        var driver = Guid.NewGuid();
        trip.SetDriverOffer(trip.MatchSessionId!, driver, k);
        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        // Act
        var result = await CreateHandler().Handle(new DriverRejectCommand(trip.Id, driver), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(k + 1, result.Value.RejectCount);
        Assert.Equal(k, result.Value.MaxAllowedRejections);
        Assert.Equal(TripStatus.Failed.ToString(), result.Value.Status);
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<DriverRejectEvent>(e => e.TripId == trip.Id && e.DriverId == driver && e.TripFailed),
            Arg.Any<CancellationToken>());
        var json = System.Text.Json.JsonSerializer.SerializeToElement(result.Value,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Equal(k + 1, json.GetProperty("rejectCount").GetInt32());
        Assert.Equal(k, json.GetProperty("maxAllowedRejections").GetInt32());
        Assert.False(json.TryGetProperty("count", out _));
    }
}
