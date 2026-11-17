using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.Repositories;
using Matching.Infrastructure.Consumers;
using NSubstitute;
using Xunit;
namespace Matching.UnitTests.Consumers;
public class TripCancelledConsumerTests
{
    [Fact]
    public async Task Consume_TripHasNotReceivedOffer_ReleasesMatchingReservation()
    {
        // Arrange
        var session = new MatchSession { Id = "507f1f77bcf86cd799439011", TripId = Guid.NewGuid(),
            OfferedDriverId = Guid.NewGuid() };
        var sessions = Substitute.For<IMatchingRepository>(); var drivers = Substitute.For<IDriverLocationRepository>();
        sessions.GetByTripIdAsync(session.TripId, Arg.Any<CancellationToken>()).Returns(session);
        var context = Substitute.For<ConsumeContext<TripCancelledEvent>>();
        context.Message.Returns(new TripCancelledEvent { TripId = session.TripId });
        // Act
        await new TripCancelledConsumer(sessions, drivers).Consume(context);
        // Assert
        Assert.Equal(MatchSessionStatus.Cancelled, session.Status);
        await drivers.Received().ReleaseAsync(session.OfferedDriverId!.Value, session.TripId, Arg.Any<CancellationToken>());
    }
}
