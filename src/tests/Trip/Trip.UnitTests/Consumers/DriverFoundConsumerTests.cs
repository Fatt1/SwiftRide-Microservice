using EventBus.Messages.Events;
using MassTransit;
using NSubstitute;
using Trip.Domain.Repositories;
using Trip.Infrastructure.Consumers;
using Trip.UnitTests.Common.Builders;
using Xunit;
namespace Trip.UnitTests.Consumers;
public class DriverFoundConsumerTests
{
    [Fact]
    public async Task Consume_DifferentOfferWhileInvitationIsActive_PublishesRejectionWithoutReplacingCurrentOffer()
    {
        // Arrange
        var trip = new TripBuilder().Build(); var driver = Guid.NewGuid();
        trip.SetDriverOffer(trip.MatchSessionId!, driver, 1); trip.RejectByDriver(driver);

        var currentDriver = Guid.NewGuid();
        trip.SetDriverOffer(trip.MatchSessionId!, currentDriver, 1);
        var trips = Substitute.For<ITripRepository>(); trips.FindByIdAsync(trip.Id).Returns(trip);
        var context = Substitute.For<ConsumeContext<DriverFoundEvent>>();
        context.Message.Returns(new DriverFoundEvent { TripId = trip.Id, MatchSessionId = trip.MatchSessionId!,
            DriverId = driver, MaxAllowedRejections = 1 });
        // Act
        await new DriverFoundConsumer(trips).Consume(context);
        // Assert
        Assert.Equal(currentDriver, trip.OfferedDriverId); Assert.Equal(1, trip.RejectCount);
        await context.Received(1).Publish(
            Arg.Is<DriverOfferRejectedEvent>(e =>
                e.TripId == trip.Id && e.DriverId == driver),
            Arg.Any<CancellationToken>());
        await trips.DidNotReceive().SaveChangesAsync();
    }
}
