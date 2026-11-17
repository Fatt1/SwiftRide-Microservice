using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Infrastructure.Consumers;
using Matching.Infrastructure.Repositories;
using Matching.IntegrationTests.Fixtures;
using NSubstitute;
using Xunit;
namespace Matching.IntegrationTests.Consumers;
public class DriverOfferConsumersTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Accept_CurrentOffer_PersistsMatchedSessionAndKeepsDriverReserved()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var sessions = new MatchingRepository(fixture.GetDatabase());
        var drivers = new DriverLocationRepository(fixture.GetDatabase());
        var driver = DriverLocation.Create(Guid.NewGuid(), "Driver A", 10.7, 106.7); await drivers.CreateAsync(driver);
        var session = new MatchSession { TripId = Guid.NewGuid(), PickupLocation = MatchSession.CreatePoint(10.7, 106.7) };
        session.OfferDriver(driver.DriverId); await sessions.CreateAsync(session);
        Assert.True(await drivers.TryReserveAsync(driver.DriverId, session.TripId));
        var context = Substitute.For<ConsumeContext<TripAcceptedEvent>>();
        context.Message.Returns(new TripAcceptedEvent { TripId = session.TripId, DriverId = driver.DriverId});
        var consumer = new TripAcceptedConsumer(sessions);
        // Act
        await consumer.Consume(context); await consumer.Consume(context);
        // Assert
        var stored = await sessions.GetByTripIdAsync(session.TripId);
        Assert.Equal(MatchSessionStatus.Matched, stored!.Status); Assert.Equal(driver.DriverId, stored.MatchedDriverId);
        Assert.Equal("Accepted", Assert.Single(stored.DriverAttempts).Outcome);
        var reserved = await drivers.GetByDriverIdAsync(driver.DriverId);
        Assert.False(reserved!.IsAvailable); Assert.Equal(session.TripId, reserved.ActiveTripId);
    }
    [Fact]
    public async Task Reject_KEqualsOne_ReleasesDriversAndFailsOnlyAfterSecondRejection()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var sessions = new MatchingRepository(fixture.GetDatabase());
        var drivers = new DriverLocationRepository(fixture.GetDatabase());
        var first = DriverLocation.Create(Guid.NewGuid(), "Driver A", 10.7, 106.7);
        var second = DriverLocation.Create(Guid.NewGuid(), "Driver B", 10.71, 106.71);
        await drivers.CreateAsync(first); await drivers.CreateAsync(second);
        var session = new MatchSession { TripId = Guid.NewGuid(), MaxAllowedRejections = 1, PickupLocation = MatchSession.CreatePoint(10.7, 106.7) };
        session.OfferDriver(first.DriverId); await sessions.CreateAsync(session);
        Assert.True(await drivers.TryReserveAsync(first.DriverId, session.TripId));
        var context = Substitute.For<ConsumeContext<DriverRejectEvent>>();
        context.Message.Returns(new DriverRejectEvent { TripId = session.TripId, DriverId = first.DriverId, RejectCount = 1 });
        var consumer = new DriverRejectedConsumer(sessions, drivers);
        // Act
        await consumer.Consume(context);
        session = (await sessions.GetByTripIdAsync(session.TripId))!;
        // Assert
        Assert.Equal(MatchSessionStatus.Searching, session.Status); Assert.Equal(1, session.RejectCount);
        Assert.Null(session.OfferedDriverId); Assert.True((await drivers.GetByDriverIdAsync(first.DriverId))!.IsAvailable);
        // Arrange: a second invitation was requested manually.
        session.OfferDriver(second.DriverId); await sessions.UpdateAsync(session);
        Assert.True(await drivers.TryReserveAsync(second.DriverId, session.TripId));
        context.Message.Returns(new DriverRejectEvent { TripId = session.TripId, DriverId = second.DriverId, RejectCount = 2, TripFailed = true });
        // Act
        await consumer.Consume(context); await consumer.Consume(context);
        var ended = await sessions.GetByTripIdAsync(session.TripId);
        // Assert
        Assert.Equal(MatchSessionStatus.NoDriver, ended!.Status); Assert.Equal(2, ended.RejectCount);
        Assert.Null(ended.OfferedDriverId); Assert.Null(ended.OfferedDriverId);
        var available = await drivers.GetByDriverIdAsync(second.DriverId);
        Assert.True(available!.IsAvailable); Assert.Null(available.ActiveTripId);
    }
}
