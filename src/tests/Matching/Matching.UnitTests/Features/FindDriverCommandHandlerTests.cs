using EventBus.Messages.Events;
using MassTransit;
using Matching.Application.Features.Matching.Commands.FindDriver;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.Repositories;
using Matching.UnitTests.Common.Builders;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared;
using Xunit;

namespace Matching.UnitTests.Features;

public class FindDriverCommandHandlerTests
{
    private readonly IMatchingRepository _matchingRepository = Substitute.For<IMatchingRepository>();
    private readonly IDriverLocationRepository _driverRepository = Substitute.For<IDriverLocationRepository>();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly ILogger<FindDriverCommandHandler> _logger = Substitute.For<ILogger<FindDriverCommandHandler>>();

    private FindDriverCommandHandler CreateHandler() =>
        new(_matchingRepository, _driverRepository, _publishEndpoint, _logger);

    [Fact]
    public async Task Handle_WhenMatchingSessionNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _matchingRepository.GetByTripIdAsync(tripId, Arg.Any<CancellationToken>())
            .Returns((MatchSession?)null);

        var handler = CreateHandler();
        var command = new FindDriverCommand(tripId, "Pickup Address", "Dropoff Address", Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task Handle_WhenMaxAttemptsReached_MarksNoDriverAndPublishesFailedEvent()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        var session = new MatchSessionBuilder()
            .WithTripId(tripId)
            .WithStatus(MatchSessionStatus.Searching)
            .WithAttempts(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()) // 3 attempts (max is 3)
            .Build();

        _matchingRepository.GetByTripIdAsync(tripId, Arg.Any<CancellationToken>())
            .Returns(session);

        var handler = CreateHandler();
        var command = new FindDriverCommand(tripId, "Pickup", "Dropoff", Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(MatchSessionStatus.NoDriver, session.Status);
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<TripMatchingFailedEvent>(e => e.TripId == tripId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoAvailableDriversFound_ReturnsFailureResult()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        var session = new MatchSessionBuilder()
            .WithTripId(tripId)
            .WithPickupLocation(10.7769, 106.7009)
            .Build();

        _matchingRepository.GetByTripIdAsync(tripId, Arg.Any<CancellationToken>())
            .Returns(session);

        _driverRepository.GetNearestAvailableDriverAsync(
                session.PickupLat, session.PickupLng, 5000, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((DriverLocation?)null);

        var handler = CreateHandler();
        var command = new FindDriverCommand(tripId, "Pickup", "Dropoff", Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No available drivers found", result.Error.Message);
    }

    [Fact]
    public async Task Handle_WhenAvailableDriverFound_AddsAttemptPublishesDriverFoundEventAndReturnsSuccess()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        var riderId = Guid.NewGuid();
        var session = new MatchSessionBuilder()
            .WithTripId(tripId)
            .WithPickupLocation(10.7769, 106.7009)
            .Build();

        var driver = new DriverLocationBuilder()
            .WithDriverId(Guid.NewGuid())
            .WithFullName("Le Van C")
            .WithLocation(10.7770, 106.7010)
            .WithAvailability(true)
            .Build();

        _matchingRepository.GetByTripIdAsync(tripId, Arg.Any<CancellationToken>())
            .Returns(session);

        _driverRepository.GetNearestAvailableDriverAsync(
                session.PickupLat, session.PickupLng, 5000, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(driver);

        var handler = CreateHandler();
        var command = new FindDriverCommand(tripId, "123 Le Loi", "456 Nguyen Hue", riderId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(driver.DriverId, result.Value.DriverId);
        Assert.Equal("Le Van C", result.Value.FullName);

        await _matchingRepository.Received(1).AddDriverAttemptAsync(session.Id, driver.DriverId, Arg.Any<CancellationToken>());
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<DriverFoundEvent>(e =>
                e.TripId == tripId &&
                e.DriverId == driver.DriverId &&
                e.RiderId == riderId &&
                e.PickupAddress == "123 Le Loi" &&
                e.DropoffAddress == "456 Nguyen Hue"),
            Arg.Any<CancellationToken>());
    }
}
