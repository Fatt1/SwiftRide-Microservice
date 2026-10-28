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

public class PaymentCompletedConsumerTests
{
    private readonly ITripRepository _tripRepository = Substitute.For<ITripRepository>();
    private readonly ILogger<PaymentCompletedConsumer> _logger = Substitute.For<ILogger<PaymentCompletedConsumer>>();

    private PaymentCompletedConsumer CreateConsumer() => new(_tripRepository, _logger);

    [Fact]
    public async Task Consume_WhenTripNotFound_LogsWarningAndDoesNotUpdate()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _tripRepository.FindByIdAsync(tripId).Returns((TripEntity?)null);

        var context = Substitute.For<ConsumeContext<PaymentCompletedEvent>>();
        context.Message.Returns(new PaymentCompletedEvent
        {
            TripId = tripId,
            PaymentId = Guid.NewGuid()
        });

        var consumer = CreateConsumer();

        // Act
        await consumer.Consume(context);

        // Assert
        await _tripRepository.DidNotReceive().Update(Arg.Any<TripEntity>());
        await _tripRepository.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task Consume_WhenTripExists_MarksTripCompletedAndSaves()
    {
        // Arrange
        var trip = new TripBuilder()
            .WithStatus(TripStatus.DroppedOff)
            .Build();

        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        var context = Substitute.For<ConsumeContext<PaymentCompletedEvent>>();
        context.Message.Returns(new PaymentCompletedEvent
        {
            TripId = trip.Id,
            PaymentId = Guid.NewGuid()
        });

        var consumer = CreateConsumer();

        // Act
        await consumer.Consume(context);

        // Assert
        Assert.Equal(TripStatus.Completed, trip.Status);
        await _tripRepository.Received(1).Update(trip);
        await _tripRepository.Received(1).SaveChangesAsync();
    }
}
