using EventBus.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
using Trip.Infrastructure.Consumers;
using Trip.UnitTests.Common.Builders;
using Xunit;
namespace Trip.UnitTests.Consumers;
public class PaymentOrderingTests
{
    [Fact]
    public async Task Consume_LateFailure_CannotOverwriteCompletedTrip()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.Completed).Build();
        var repo = Substitute.For<ITripRepository>(); repo.FindByIdAsync(trip.Id).Returns(trip);
        var context = Substitute.For<ConsumeContext<PaymentFailedEvent>>();
        context.Message.Returns(new PaymentFailedEvent { TripId = trip.Id, CorrelationId = Guid.NewGuid(),
            RiderId = trip.RiderId, Amount = trip.EstimatedFare!.Value, Reason = "late failure" });
        // Act
        await new PaymentFailedConsumer(repo, NullLogger<PaymentFailedConsumer>.Instance).Consume(context);
        // Assert
        Assert.Equal(TripStatus.Completed, trip.Status); Assert.Null(trip.FailureReason);
    }
    [Fact]
    public async Task Consume_LateFailureWhileNextAttemptIsPending_DoesNotChangeTrip()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.PaymentPending).Build();
        var repo = Substitute.For<ITripRepository>(); repo.FindByIdAsync(trip.Id).Returns(trip);
        var context = Substitute.For<ConsumeContext<PaymentFailedEvent>>();
        context.Message.Returns(new PaymentFailedEvent { TripId = trip.Id, CorrelationId = Guid.NewGuid(),
            RiderId = trip.RiderId, Amount = trip.EstimatedFare!.Value, Reason = "stale" });
        // Act
        await new PaymentFailedConsumer(repo, NullLogger<PaymentFailedConsumer>.Instance).Consume(context);
        // Assert
        Assert.Equal(TripStatus.PaymentPending, trip.Status); Assert.Null(trip.FailureReason);
    }
    [Fact]
    public async Task Consume_ExhaustedAutomaticRetries_KeepsPendingWithoutChangingFailureReason()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.PaymentPending).Build(); var key = Guid.NewGuid();
        var repo = Substitute.For<ITripRepository>(); repo.FindByIdAsync(trip.Id).Returns(trip);
        var fault = Substitute.For<Fault<TripDropOffEvent>>();
        fault.Message.Returns(new TripDropOffEvent { IdempotencyKey = key, TripId = trip.Id,
            RiderId = trip.RiderId, DriverId = trip.DriverId!.Value, Amount = trip.EstimatedFare!.Value });
        var context = Substitute.For<ConsumeContext<Fault<TripDropOffEvent>>>(); context.Message.Returns(fault);
        // Act
        await new PaymentFaultConsumer(NullLogger<PaymentFaultConsumer>.Instance).Consume(context);
        // Assert
        Assert.Equal(TripStatus.PaymentPending, trip.Status);
        Assert.Null(trip.FailureReason);
    }

}
