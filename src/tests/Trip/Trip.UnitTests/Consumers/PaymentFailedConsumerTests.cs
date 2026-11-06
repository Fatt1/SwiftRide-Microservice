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

public class PaymentFailedConsumerTests
{
    private readonly ITripRepository _tripRepository = Substitute.For<ITripRepository>();
    private readonly ILogger<PaymentFailedConsumer> _logger = Substitute.For<ILogger<PaymentFailedConsumer>>();

    private PaymentFailedConsumer CreateConsumer() => new(_tripRepository, _logger);

    [Fact]
    public async Task Consume_WhenTripNotFound_LogsWarningAndDoesNotUpdate()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _tripRepository.FindByIdAsync(tripId).Returns((TripEntity?)null);

        var context = Substitute.For<ConsumeContext<PaymentFailedEvent>>();
        context.Message.Returns(new PaymentFailedEvent
        {
            TripId = tripId,
            Reason = "Insufficient funds"
        });

        var consumer = CreateConsumer();

        // Act
        await consumer.Consume(context);

        // Assert
        await _tripRepository.DidNotReceive().Update(Arg.Any<TripEntity>());
        await _tripRepository.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task Consume_WhenTripExists_MarksTripPaymentFailedWithReasonAndSaves()
    {
        // Arrange
        var trip = new TripBuilder()
            .WithStatus(TripStatus.DroppedOff)
            .Build();

        _tripRepository.FindByIdAsync(trip.Id).Returns(trip);

        const string failureReason = "Payment gateway timeout";
        var context = Substitute.For<ConsumeContext<PaymentFailedEvent>>();
        context.Message.Returns(new PaymentFailedEvent
        {
            TripId = trip.Id,
            Reason = failureReason
        });

        var consumer = CreateConsumer();

        // Act
        await consumer.Consume(context);

        // Assert
        Assert.Equal(TripStatus.PaymentFailed, trip.Status);
        Assert.Equal(failureReason, trip.PaymentFailureReason);
        await _tripRepository.Received(1).Update(trip);
        await _tripRepository.Received(1).SaveChangesAsync();
    }
}
