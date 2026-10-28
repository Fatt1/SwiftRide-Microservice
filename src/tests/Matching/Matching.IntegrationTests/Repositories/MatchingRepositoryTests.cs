using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Infrastructure.Repositories;
using Matching.IntegrationTests.Common.Builders;
using Matching.IntegrationTests.Fixtures;
using Xunit;

namespace Matching.IntegrationTests.Repositories;

public class MatchingRepositoryTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private MatchingRepository CreateRepository() => new(fixture.GetDatabase());

    [Fact]
    public async Task CreateAsync_WithValidSession_PersistsSessionInDatabase()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var repo = CreateRepository();
        var session = new MatchSessionBuilder()
            .WithDistance(5.2, 14.0)
            .Build();

        // Act
        await repo.CreateAsync(session);

        // Assert
        var persisted = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(persisted);
        Assert.Equal(session.TripId, persisted.TripId);
        Assert.Equal(5.2, persisted.DistanceKm);
        Assert.Equal(14.0, persisted.EstimatedMinutes);
    }

    [Fact]
    public async Task GetByTripIdAsync_WhenTripExists_ReturnsMatchingSession()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var repo = CreateRepository();
        var tripId = Guid.NewGuid();
        var session = new MatchSessionBuilder()
            .WithTripId(tripId)
            .Build();
        await repo.CreateAsync(session);

        // Act
        var result = await repo.GetByTripIdAsync(tripId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tripId, result.TripId);
    }

    [Fact]
    public async Task AddDriverAttemptAsync_PushesDriverAttemptToMongoList()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var repo = CreateRepository();
        var session = new MatchSessionBuilder().Build();
        await repo.CreateAsync(session);

        var driverId = Guid.NewGuid();

        // Act
        await repo.AddDriverAttemptAsync(session.Id, driverId);

        // Assert
        var updated = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(updated);
        Assert.Single(updated.DriverAttempts);
        Assert.Equal(driverId, updated.DriverAttempts[0].DriverId);
    }

    [Fact]
    public async Task UpdateAsync_WhenStatusChanges_UpdatesDocumentInDatabase()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var repo = CreateRepository();
        var session = new MatchSessionBuilder()
            .WithStatus(MatchSessionStatus.Searching)
            .Build();
        await repo.CreateAsync(session);

        var matchedDriverId = Guid.NewGuid();
        session.MarkMatched(matchedDriverId);

        // Act
        await repo.UpdateAsync(session);

        // Assert
        var updated = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(updated);
        Assert.Equal(MatchSessionStatus.Matched, updated.Status);
        Assert.Equal(matchedDriverId, updated.MatchedDriverId);
    }
}
