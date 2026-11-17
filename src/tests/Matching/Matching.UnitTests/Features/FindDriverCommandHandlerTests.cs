using EventBus.Messages.Events;
using MassTransit;
using Matching.Application.Abstractions;
using Matching.Application.Features.Matching.Commands.FindDriver;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.Repositories;
using NSubstitute;
using Shared;
using Xunit;
namespace Matching.UnitTests.Features;
public class FindDriverCommandHandlerTests
{
    private readonly IMatchingRepository sessions = Substitute.For<IMatchingRepository>();
    private readonly IDriverLocationRepository drivers = Substitute.For<IDriverLocationRepository>();
    private readonly IPublishEndpoint publisher = Substitute.For<IPublishEndpoint>();
    private MediatR.IRequestHandler<FindDriverCommand, Result<FindDriverResult>> Handler() => global::Matching.UnitTests.ValidatedHandlers.FindDriver(sessions, drivers, publisher);
    private MatchSession ArrangeSession(int rejected = 0, int k = 1)
    {
        var s = new MatchSession { Id = "507f1f77bcf86cd799439011", TripId = Guid.NewGuid(),
            PickupLocation = MatchSession.CreatePoint(10.7, 106.7), MaxAllowedRejections = k};
        for (var i = 0; i < rejected; i++) s.DriverAttempts.Add(new DriverAttempt { DriverId = Guid.NewGuid(), Outcome = "Rejected"});
        sessions.GetByTripIdAsync(s.TripId, Arg.Any<CancellationToken>()).Returns(s);
        return s;
    }
    private DriverLocation ArrangeDriver(MatchSession s)
    {
        var d = DriverLocation.Create(Guid.NewGuid(), "Driver", 10.7, 106.7);
        drivers.GetNearestAvailableDriverAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(d);
        drivers.GetByDriverIdAsync(d.DriverId, Arg.Any<CancellationToken>()).Returns(d);
        drivers.TryReserveAsync(d.DriverId, s.TripId, Arg.Any<CancellationToken>()).Returns(true);
        return d;
    }
    [Fact]
    public async Task Handle_MissingTrip_ReturnsNotFound()
    {
        // Arrange
        // Act
        var result = await Handler().Handle(new FindDriverCommand(Guid.NewGuid()), default);
        // Assert
        Assert.IsType<NotFoundError>(result.Error);
    }
    [Fact]
    public async Task Handle_AtK_ReoffersDifferentDriverAndStoresInvitedDriver()
    {
        // Arrange
        var s = ArrangeSession(1, 1); var d = ArrangeDriver(s); var rejected = s.DriverAttempts[0].DriverId;
        // Act
        var result = await Handler().Handle(new FindDriverCommand(s.TripId), default);
        // Assert
        Assert.True(result.IsSuccess); Assert.Equal(d.DriverId, result.Value.DriverId);
        Assert.NotEqual(Guid.Empty, result.Value.DriverId);
        Assert.Equal(result.Value.DriverId, s.OfferedDriverId);
        await drivers.Received().GetNearestAvailableDriverAsync(Arg.Any<double>(), Arg.Any<double>(), 5000,
            Arg.Is<List<Guid>>(ids => ids.Contains(rejected)), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_SessionEndedByTrip_DoesNotInviteAnotherDriver()
    {
        // Arrange
        var s = ArrangeSession(2, 1);
        s.MarkNoDriver();
        // Act
        var result = await Handler().Handle(new FindDriverCommand(s.TripId), default);
        // Assert
        Assert.False(result.IsSuccess); Assert.Equal(MatchSessionStatus.NoDriver, s.Status);
        await publisher.DidNotReceive().Publish(Arg.Any<DriverFoundEvent>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_ExistingOffer_ReplaysWithoutNewAttempt()
    {
        // Arrange
        var s = ArrangeSession(); var d = ArrangeDriver(s);
        s.OfferedDriverId = d.DriverId;
        s.DriverAttempts.Add(new DriverAttempt { DriverId = d.DriverId});
        // Act
        var result = await Handler().Handle(new FindDriverCommand(s.TripId), default);
        // Assert
        Assert.True(result.IsSuccess); Assert.Equal(d.DriverId, result.Value.DriverId); Assert.Single(s.DriverAttempts);
        await drivers.DidNotReceive().TryReserveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_NoDriver_ReturnsErrorAndAllowsManualRetry()
    {
        // Arrange
        var s = ArrangeSession();
        // Act
        var result = await Handler().Handle(new FindDriverCommand(s.TripId), default);
        // Assert
        Assert.False(result.IsSuccess); Assert.Equal(MatchSessionStatus.Searching, s.Status);
        await publisher.DidNotReceive().Publish(Arg.Any<TripMatchingFailedEvent>(), Arg.Any<CancellationToken>());
        await sessions.DidNotReceive().UpdateAsync(Arg.Any<MatchSession>(), Arg.Any<CancellationToken>());

        // Arrange: a driver is now available; the user explicitly retries Matching.
        var driver = ArrangeDriver(s);
        // Act
        var retry = await Handler().Handle(new FindDriverCommand(s.TripId), default);
        // Assert
        Assert.True(retry.IsSuccess); Assert.Equal(driver.DriverId, retry.Value.DriverId);
    }
    [Fact]
    public async Task Handle_SessionAlreadyMatched_DoesNotCreateOffer()
    {
        // Arrange
        var s = ArrangeSession();
        s.Status = MatchSessionStatus.Matched;
        // Act
        var result = await Handler().Handle(new FindDriverCommand(s.TripId), default);
        // Assert
        Assert.IsType<ConflictError>(result.Error);
        await publisher.DidNotReceive().Publish(Arg.Any<DriverFoundEvent>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_NearestDriverWasReserved_InvitesNextDriverWithoutPhantomAttempt()
    {
        // Arrange
        var session = ArrangeSession(1, 1);
        var next = ArrangeDriver(session);
        var nearest = DriverLocation.Create(Guid.NewGuid(), "Already taken", 10.7, 106.7);
        drivers.GetNearestAvailableDriverAsync(Arg.Any<double>(), Arg.Any<double>(), 5000,
            Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(call =>
                ((List<Guid>)call[3]).Contains(nearest.DriverId) ? next : nearest);
        drivers.TryReserveAsync(nearest.DriverId, session.TripId, Arg.Any<CancellationToken>()).Returns(false);
        drivers.TryReserveAsync(next.DriverId, session.TripId, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Assert.Null(session.OfferedDriverId);
            Assert.Single(session.DriverAttempts);

            return true;
        });
        // Act
        var result = await Handler().Handle(new FindDriverCommand(session.TripId), default);
        // Assert
        Assert.True(result.IsSuccess); Assert.Equal(next.DriverId, result.Value.DriverId);
        Assert.Equal(1, session.RejectCount);
        Assert.Equal(2, session.DriverAttempts.Count);
        Assert.DoesNotContain(session.DriverAttempts, x => x.DriverId == nearest.DriverId);
        await publisher.Received(1).Publish(Arg.Is<DriverFoundEvent>(e => e.DriverId == next.DriverId), Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().Publish(Arg.Any<TripMatchingFailedEvent>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_AllCandidatesWereReserved_ReturnsErrorWithoutEndingSearch()
    {
        // Arrange
        var session = ArrangeSession();
        var nearest = DriverLocation.Create(Guid.NewGuid(), "Already taken", 10.7, 106.7);
        drivers.GetNearestAvailableDriverAsync(Arg.Any<double>(), Arg.Any<double>(), 5000,
            Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(call =>
                ((List<Guid>)call[3]).Contains(nearest.DriverId) ? null : nearest);
        drivers.TryReserveAsync(nearest.DriverId, session.TripId, Arg.Any<CancellationToken>()).Returns(false);
        // Act
        var result = await Handler().Handle(new FindDriverCommand(session.TripId), default);
        // Assert
        Assert.False(result.IsSuccess); Assert.Equal(MatchSessionStatus.Searching, session.Status);
        Assert.Empty(session.DriverAttempts);
        Assert.Equal(0, session.RejectCount); Assert.Null(session.OfferedDriverId);
        await publisher.DidNotReceive().Publish(Arg.Any<DriverFoundEvent>(), Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().Publish(Arg.Any<TripMatchingFailedEvent>(), Arg.Any<CancellationToken>());
    }

}
