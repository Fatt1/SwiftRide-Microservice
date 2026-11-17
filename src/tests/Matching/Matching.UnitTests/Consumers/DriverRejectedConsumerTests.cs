using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.Repositories;
using Matching.Infrastructure.Consumers;
using NSubstitute;
using Xunit;
namespace Matching.UnitTests.Consumers;
public class DriverRejectedConsumerTests
{
    [Theory]
    [InlineData(1, false, MatchSessionStatus.Searching)]
    [InlineData(2, true, MatchSessionStatus.NoDriver)]
    [InlineData(2, false, MatchSessionStatus.Searching)]
    [InlineData(1, true, MatchSessionStatus.NoDriver)]
    public async Task Consume_CurrentDriver_RecordsRejectionAndUsesTripFailureDecision(int count, bool tripFailed, MatchSessionStatus expected)
    {
        // Arrange
        var session = new MatchSession { TripId = Guid.NewGuid(), MaxAllowedRejections = 1};
        for (var i = 1; i < count; i++) session.DriverAttempts.Add(new DriverAttempt { DriverId = Guid.NewGuid(), Outcome = "Rejected" });
        var driverId = Guid.NewGuid(); session.OfferDriver(driverId);
        var sessions = Substitute.For<IMatchingRepository>(); var drivers = Substitute.For<IDriverLocationRepository>();
        sessions.GetByTripIdAsync(session.TripId, Arg.Any<CancellationToken>()).Returns(session);
        var context = Substitute.For<ConsumeContext<DriverRejectEvent>>();
        context.Message.Returns(new DriverRejectEvent { TripId = session.TripId, DriverId = driverId, RejectCount = count, TripFailed = tripFailed });
        var consumer = new DriverRejectedConsumer(sessions, drivers);
        // Act
        await consumer.Consume(context); await consumer.Consume(context);
        // Assert
        Assert.Equal(expected, session.Status); Assert.Null(session.OfferedDriverId);
        Assert.Equal(count, session.RejectCount); Assert.Equal("Rejected", session.DriverAttempts.Last().Outcome);
        await sessions.Received(1).UpdateAsync(session, Arg.Any<CancellationToken>());
        await drivers.Received(2).ReleaseAsync(driverId, session.TripId, Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Consume_MissingSession_StillReleasesTheEventReservation()
    {
        // Arrange
        var sessions = Substitute.For<IMatchingRepository>(); var drivers = Substitute.For<IDriverLocationRepository>();
        var message = new DriverRejectEvent { TripId = Guid.NewGuid(), DriverId = Guid.NewGuid()};
        var context = Substitute.For<ConsumeContext<DriverRejectEvent>>(); context.Message.Returns(message);
        // Act
        await new DriverRejectedConsumer(sessions, drivers).Consume(context);
        // Assert
        await sessions.DidNotReceive().UpdateAsync(Arg.Any<MatchSession>(), Arg.Any<CancellationToken>());
        await drivers.Received(1).ReleaseAsync(message.DriverId, message.TripId, Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Consume_OldOffer_DoesNotClearCurrentOfferAndOnlyReleasesOldReservation()
    {
        // Arrange
        var session = new MatchSession { TripId = Guid.NewGuid() };
        var currentDriver = Guid.NewGuid();
        session.OfferDriver(currentDriver);
        var sessions = Substitute.For<IMatchingRepository>(); var drivers = Substitute.For<IDriverLocationRepository>();
        sessions.GetByTripIdAsync(session.TripId, Arg.Any<CancellationToken>()).Returns(session);
        var message = new DriverRejectEvent { TripId = session.TripId, DriverId = Guid.NewGuid()};
        var context = Substitute.For<ConsumeContext<DriverRejectEvent>>(); context.Message.Returns(message);
        // Act
        await new DriverRejectedConsumer(sessions, drivers).Consume(context);
        // Assert
        Assert.Equal(currentDriver, session.OfferedDriverId); Assert.Equal(0, session.RejectCount);
        await sessions.DidNotReceive().UpdateAsync(Arg.Any<MatchSession>(), Arg.Any<CancellationToken>());
        await drivers.Received(1).ReleaseAsync(message.DriverId, message.TripId, Arg.Any<CancellationToken>());
    }
}
