using EventBus.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
using Trip.Infrastructure.Consumers;
using Trip.UnitTests.Common.Builders;
using Xunit;

namespace Trip.UnitTests.Consumers;

public class TripMatchingFailedConsumerTests
{
    private readonly ITripRepository _tripRepository = Substitute.For<ITripRepository>();
    private readonly ILogger<TripMatchingFailedConsumer> _logger = Substitute.For<ILogger<TripMatchingFailedConsumer>>();

    private TripMatchingFailedConsumer CreateConsumer() => new(_tripRepository, _logger);

    [Fact]
    public async Task Consume_WhenTripNotFound_LogsWarningAndDoesNotUpdate()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _tripRepository.FindByIdAsync(tripId).Returns((TripEntity?)null);

        var context = Substitute.For<ConsumeContext<TripMatchingFailedEvent>>();
        context.Message.Returns(new TripMatchingFailedEvent
        {
            TripId = tripId
        });

        var consumer = CreateConsumer();

        // Act
        await consumer.Consume(context);

        // Assert
        await _tripRepository.DidNotReceive().Update(Arg.Any<TripEntity>());
        await _tripRepository.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task Consume_WhenTripIsRequested_MarksDriverRejectedAndSaves()
    {
        // Arrange
        var trip = new TripBuilder()
            .WithStatus(TripStatus.Requested)
            .Build();

        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var context = Substitute.For<ConsumeContext<TripMatchingFailedEvent>>();
        context.Message.Returns(new TripMatchingFailedEvent
        {
            TripId = trip.Id
        });

        var consumer = CreateConsumer();

        // Act
        await consumer.Consume(context);

        // Assert
        Assert.Equal(TripStatus.DriverRejected, trip.Status);
        await _tripRepository.Received(1).Update(trip);
        await _tripRepository.Received(1).SaveChangesAsync();
    }

    [Theory]
    [InlineData(TripStatus.DriverAccepted)]
    [InlineData(TripStatus.PickedUp)]
    [InlineData(TripStatus.Completed)]
    public async Task Consume_WhenTripIsNotRequested_LogsWarningAndSkipsUpdate(TripStatus status)
    {
        // Arrange
        var trip = new TripBuilder()
            .WithStatus(status)
            .Build();

        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var context = Substitute.For<ConsumeContext<TripMatchingFailedEvent>>();
        context.Message.Returns(new TripMatchingFailedEvent
        {
            TripId = trip.Id
        });

        var consumer = CreateConsumer();

        // Act
        await consumer.Consume(context);

        // Assert
        Assert.Equal(status, trip.Status);
        await _tripRepository.DidNotReceive().Update(Arg.Any<TripEntity>());
        await _tripRepository.DidNotReceive().SaveChangesAsync();
    }
}
