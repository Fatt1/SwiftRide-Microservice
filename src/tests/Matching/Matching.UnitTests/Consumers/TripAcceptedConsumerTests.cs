using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.Repositories;
using Matching.Infrastructure.Consumers;
using NSubstitute;
using Xunit;
namespace Matching.UnitTests.Consumers;
public class TripAcceptedConsumerTests
{
    [Fact]
    public async Task Consume_CurrentOffer_MarksMatchedAndDuplicateDoesNotSaveAgain()
    {
        // Arrange
        var session = new MatchSession { TripId = Guid.NewGuid() };
        var driverId = Guid.NewGuid(); session.OfferDriver(driverId);
        var sessions = Substitute.For<IMatchingRepository>();
        sessions.GetByTripIdAsync(session.TripId, Arg.Any<CancellationToken>()).Returns(session);
        var context = Substitute.For<ConsumeContext<TripAcceptedEvent>>();
        context.Message.Returns(new TripAcceptedEvent { TripId = session.TripId, DriverId = driverId});
        var consumer = new TripAcceptedConsumer(sessions);
        // Act
        await consumer.Consume(context); await consumer.Consume(context);
        // Assert
        Assert.Equal(MatchSessionStatus.Matched, session.Status); Assert.Equal(driverId, session.MatchedDriverId);
        Assert.Equal("Accepted", Assert.Single(session.DriverAttempts).Outcome);
        await sessions.Received(1).UpdateAsync(session, Arg.Any<CancellationToken>());
    }
    [Theory]
    [InlineData(MatchSessionStatus.Matched)]
    [InlineData(MatchSessionStatus.Cancelled)]
    [InlineData(MatchSessionStatus.NoDriver)]
    public async Task Consume_EndedSession_DoesNotChangeState(MatchSessionStatus status)
    {
        // Arrange
        var session = new MatchSession { TripId = Guid.NewGuid() };
        session.OfferDriver(Guid.NewGuid()); session.Status = status;
        var sessions = Substitute.For<IMatchingRepository>(); sessions.GetByTripIdAsync(session.TripId, Arg.Any<CancellationToken>()).Returns(session);
        var context = Substitute.For<ConsumeContext<TripAcceptedEvent>>();
        context.Message.Returns(new TripAcceptedEvent { TripId = session.TripId});
        // Act
        await new TripAcceptedConsumer(sessions).Consume(context);
        // Assert
        Assert.Equal(status, session.Status); Assert.Equal("Offered", Assert.Single(session.DriverAttempts).Outcome);
        await sessions.DidNotReceive().UpdateAsync(Arg.Any<MatchSession>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Consume_UnknownTripOrOldOffer_DoesNotSave()
    {
        // Arrange
        var sessions = Substitute.For<IMatchingRepository>();
        var context = Substitute.For<ConsumeContext<TripAcceptedEvent>>();
        var tripId = Guid.NewGuid(); context.Message.Returns(new TripAcceptedEvent { TripId = tripId});
        var consumer = new TripAcceptedConsumer(sessions);
        // Act: no session, then a session with a different invitation.
        await consumer.Consume(context);
        var session = new MatchSession { TripId = tripId }; session.OfferDriver(Guid.NewGuid());
        sessions.GetByTripIdAsync(tripId, Arg.Any<CancellationToken>()).Returns(session);
        await consumer.Consume(context);
        // Assert
        Assert.Equal(MatchSessionStatus.Searching, session.Status);
        Assert.Equal("Offered", Assert.Single(session.DriverAttempts).Outcome);
        await sessions.DidNotReceive().UpdateAsync(Arg.Any<MatchSession>(), Arg.Any<CancellationToken>());
    }
}
