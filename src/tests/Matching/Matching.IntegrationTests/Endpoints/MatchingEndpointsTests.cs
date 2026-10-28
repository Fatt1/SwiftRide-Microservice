using System.Net;
using System.Net.Http.Json;
using Matching.Application.Features.Matching.Commands.FindDriver;
using Matching.Application.Features.Matching.Dtos;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.IntegrationTests.Common.Builders;
using Matching.IntegrationTests.Fixtures;
using MongoDB.Driver;
using Xunit;

namespace Matching.IntegrationTests.Endpoints;

public class MatchingEndpointsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task GetAllDriverLocations_WhenLocationsExist_ReturnsOkWithDriverList()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var database = fixture.GetDatabase();
        var driverCol = database.GetCollection<DriverLocation>("driver_locations");

        var driver1 = new DriverLocationBuilder()
            .WithFullName("Nguyen Van A")
            .WithLocation(10.7769, 106.7009)
            .WithAvailability(true)
            .Build();

        var driver2 = new DriverLocationBuilder()
            .WithFullName("Tran Van B")
            .WithLocation(10.7800, 106.7100)
            .WithAvailability(false)
            .Build();

        await driverCol.InsertManyAsync([driver1, driver2]);

        // Act
        var response = await _client.GetAsync("/api/v1/matching/drivers/locations");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<DriverLocationDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.FullName == "Nguyen Van A" && d.IsAvailable);
        Assert.Contains(result, d => d.FullName == "Tran Van B" && !d.IsAvailable);
    }

    [Fact]
    public async Task GetAllMatchSessions_WhenSessionsExist_ReturnsOkWithSessionList()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var database = fixture.GetDatabase();
        var sessionCol = database.GetCollection<MatchSession>("match_sessions");

        var session1 = new MatchSessionBuilder().WithStatus(MatchSessionStatus.Searching).Build();
        var session2 = new MatchSessionBuilder().WithStatus(MatchSessionStatus.Matched).Build();
        await sessionCol.InsertManyAsync([session1, session2]);

        // Act
        var response = await _client.GetAsync("/api/v1/matching/matchings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<MatchSessionDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetSessionsAlias_WhenSessionsExist_ReturnsOkWithSessionList()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var database = fixture.GetDatabase();
        var sessionCol = database.GetCollection<MatchSession>("match_sessions");

        var session = new MatchSessionBuilder().Build();
        await sessionCol.InsertOneAsync(session);

        // Act
        var response = await _client.GetAsync("/api/v1/matching/sessions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<MatchSessionDto>>();
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetMatchSessionById_WhenSessionExists_ReturnsOkWithSession()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var database = fixture.GetDatabase();
        var sessionCol = database.GetCollection<MatchSession>("match_sessions");

        var session = new MatchSessionBuilder()
            .WithDistance(6.5, 18.0)
            .Build();
        await sessionCol.InsertOneAsync(session);

        // Act
        var response = await _client.GetAsync($"/api/v1/matching/matchings/{session.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MatchSessionDto>();
        Assert.NotNull(result);
        Assert.Equal(session.Id, result.Id);
        Assert.Equal(session.TripId, result.TripId);
        Assert.Equal(6.5, result.DistanceKm);
    }

    [Fact]
    public async Task GetMatchSessionById_WhenSessionNotFound_ReturnsNotFound()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        const string nonExistentId = "507f1f77bcf86cd799439099";

        // Act
        var response = await _client.GetAsync($"/api/v1/matching/matchings/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Match_WithAvailableDriverNearby_ReturnsOkWithMatchedDriver()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var database = fixture.GetDatabase();
        var sessionCol = database.GetCollection<MatchSession>("match_sessions");
        var driverCol = database.GetCollection<DriverLocation>("driver_locations");

        var tripId = Guid.NewGuid();
        const double pickupLat = 10.7725;
        const double pickupLon = 106.6980;

        var session = new MatchSessionBuilder()
            .WithTripId(tripId)
            .WithPickupLocation(pickupLat, pickupLon)
            .WithStatus(MatchSessionStatus.Searching)
            .Build();
        await sessionCol.InsertOneAsync(session);

        // Driver located 200m away
        var driver = new DriverLocationBuilder()
            .WithFullName("Ta Van Driver")
            .WithLocation(pickupLat + 0.001, pickupLon + 0.001)
            .WithAvailability(true)
            .Build();
        await driverCol.InsertOneAsync(driver);

        var command = new FindDriverCommand(
            TripId: tripId,
            PickupAddress: "Ben Thanh Market",
            DropoffAddress: "Landmark 81",
            RiderId: Guid.NewGuid()
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/matching/match", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<FindDriverResult>();
        Assert.NotNull(result);
        Assert.Equal(driver.DriverId, result.DriverId);
        Assert.Equal("Ta Van Driver", result.FullName);

        // Verify driver attempt was recorded in real MongoDB database
        var updatedSession = await sessionCol.Find(s => s.TripId == tripId).FirstOrDefaultAsync();
        Assert.NotNull(updatedSession);
        Assert.Contains(updatedSession.DriverAttempts, a => a.DriverId == driver.DriverId);
    }

    [Fact]
    public async Task Match_WhenTripNotFound_ReturnsNotFound()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var command = new FindDriverCommand(
            TripId: Guid.NewGuid(),
            PickupAddress: "Nowhere",
            DropoffAddress: "Somewhere"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/matching/match", command);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Match_WhenNoDriverNearby_ReturnsBadRequest()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var database = fixture.GetDatabase();
        var sessionCol = database.GetCollection<MatchSession>("match_sessions");

        var tripId = Guid.NewGuid();
        var session = new MatchSessionBuilder()
            .WithTripId(tripId)
            .WithPickupLocation(10.7725, 106.6980)
            .Build();
        await sessionCol.InsertOneAsync(session);

        var command = new FindDriverCommand(
            TripId: tripId,
            PickupAddress: "Somewhere in desert",
            DropoffAddress: "Far away"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/matching/match", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
