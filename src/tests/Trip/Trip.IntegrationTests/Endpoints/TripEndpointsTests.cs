using System.Net;
using System.Net.Http.Json;
using EventBus.Messages.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shared.Enums.Payments;
using Trip.API.DTOs;
using Trip.Application.DTOs;
using Trip.Application.Features.Trips.Commands.CreateTrip;
using Trip.Domain.Enums;
using Trip.IntegrationTests.Common.Builders;
using Trip.IntegrationTests.Fixtures;
using Xunit;

namespace Trip.IntegrationTests.Endpoints;

public class TripEndpointsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task CreateTrip_WithValidCommand_ReturnsCreatedAndPersistsTrip()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var riderId = Guid.NewGuid();
        var command = new CreateTripCommand(
            RiderId: riderId,
            PickupAddress: "227 Nguyen Van Cu, D5",
            DropoffAddress: "Ben Thanh Market, D1",
            PickupLat: 10.762622,
            PickupLng: 106.682373,
            DropoffLat: 10.772545,
            DropoffLng: 106.698032,
            HasToll: false
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/trips", command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreateTripResponse>();
        Assert.NotNull(created);
        Assert.Equal(riderId, created.RiderId);
        Assert.Equal(TripStatus.Requested.ToString(), created.Status);
        Assert.Contains($"/api/v1/trips/{created.TripId}", response.Headers.Location?.ToString());

        using var db = fixture.GetDbContext();
        var persisted = await db.Trips.FindAsync(created.TripId);
        Assert.NotNull(persisted);
        Assert.Equal(riderId, persisted.RiderId);
    }

    [Fact]
    public async Task GetAllTrips_WhenTripsExist_ReturnsOkWithTripList()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var trip1 = new TripBuilder().Build();
        var trip2 = new TripBuilder().Build();
        await db.Trips.AddRangeAsync(trip1, trip2);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/trips");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<TripDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllTrips_WithRiderIdFilter_ReturnsOnlyMatchingTrips()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var targetRiderId = Guid.NewGuid();
        var trip1 = new TripBuilder().WithRiderId(targetRiderId).Build();
        var trip2 = new TripBuilder().WithRiderId(Guid.NewGuid()).Build();
        await db.Trips.AddRangeAsync(trip1, trip2);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/trips?riderId={targetRiderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<TripDto>>();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(targetRiderId, result[0].RiderId);
    }

    [Fact]
    public async Task GetTripById_WhenTripExists_ReturnsOkWithTripDetails()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var trip = new TripBuilder()
            .WithEstimatedFare(75000m)
            .WithDistanceKm(8.0m)
            .Build();
        await db.Trips.AddAsync(trip);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/trips/{trip.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TripDto>();
        Assert.NotNull(result);
        Assert.Equal(trip.Id, result.Id);
        Assert.Equal(75000m, result.EstimatedFare);
    }

    [Fact]
    public async Task GetTripById_WhenNotFound_ReturnsNotFoundStatusCode()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/trips/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DriverAccept_WhenTripRequested_ReturnsOkAndPublishesEvent()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var trip = new TripBuilder().WithStatus(TripStatus.Requested).Build();
        await db.Trips.AddAsync(trip);
        await db.SaveChangesAsync();

        var driverId = Guid.NewGuid();
        var request = new DriverAcceptRequest(driverId);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/driver-accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyDb = fixture.GetDbContext();
        var updated = await verifyDb.Trips.FindAsync(trip.Id);
        Assert.NotNull(updated);
        Assert.Equal(TripStatus.DriverAccepted, updated.Status);
        Assert.Equal(driverId, updated.DriverId);

        await fixture.PublishEndpointMock.Received().Publish(
            Arg.Is<TripAcceptedEvent>(e => e.TripId == trip.Id && e.DriverId == driverId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DriverAccept_WhenTripAlreadyAccepted_ReturnsConflictStatusCode()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var trip = new TripBuilder().WithStatus(TripStatus.DriverAccepted).Build();
        await db.Trips.AddAsync(trip);
        await db.SaveChangesAsync();

        var request = new DriverAcceptRequest(Guid.NewGuid());

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/driver-accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DriverReject_WhenTripRequested_ReturnsOkAndPublishesEvent()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var trip = new TripBuilder().WithStatus(TripStatus.Requested).Build();
        await db.Trips.AddAsync(trip);
        await db.SaveChangesAsync();

        var driverId = Guid.NewGuid();
        var request = new DriverRejectRequest(driverId);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/driver-reject", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await fixture.PublishEndpointMock.Received().Publish(
            Arg.Is<DriverRejectEvent>(e => e.TripId == trip.Id && e.DriverId == driverId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PickUp_WhenTripDriverAccepted_ReturnsOkAndUpdatesStatus()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var trip = new TripBuilder().WithStatus(TripStatus.DriverAccepted).Build();
        await db.Trips.AddAsync(trip);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync($"/api/v1/trips/{trip.Id}/pickup", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyDb = fixture.GetDbContext();
        var updated = await verifyDb.Trips.FindAsync(trip.Id);
        Assert.NotNull(updated);
        Assert.Equal(TripStatus.PickedUp, updated.Status);
    }

    [Fact]
    public async Task PickUp_WhenTripRequested_ReturnsConflictStatusCode()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var trip = new TripBuilder().WithStatus(TripStatus.Requested).Build();
        await db.Trips.AddAsync(trip);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync($"/api/v1/trips/{trip.Id}/pickup", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DropOff_WhenTripPickedUp_ReturnsOkPublishesPaymentEventAndUpdatesTrip()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var driverId = Guid.NewGuid();
        var trip = new TripBuilder()
            .WithDriverId(driverId)
            .WithEstimatedFare(85000m)
            .WithStatus(TripStatus.PickedUp)
            .Build();
        await db.Trips.AddAsync(trip);
        await db.SaveChangesAsync();

        var request = new DropOffRequest(PaymentMethod.Wallet);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/dropoff", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyDb = fixture.GetDbContext();
        var updated = await verifyDb.Trips.FindAsync(trip.Id);
        Assert.NotNull(updated);
        Assert.Equal(TripStatus.DroppedOff, updated.Status);
        Assert.Equal(85000m, updated.FinalFare);

        await fixture.PublishEndpointMock.Received().Publish(
            Arg.Is<TripDropOffEvent>(e =>
                e.TripId == trip.Id &&
                e.Amount == 85000m &&
                e.PaymentMethod == PaymentMethod.Wallet),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DropOff_WhenTripNotPickedUp_ReturnsConflictStatusCode()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var trip = new TripBuilder().WithStatus(TripStatus.Requested).Build();
        await db.Trips.AddAsync(trip);
        await db.SaveChangesAsync();

        var request = new DropOffRequest(PaymentMethod.Wallet);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/dropoff", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_WhenTripRequested_ReturnsOkAndCancelsTrip()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var trip = new TripBuilder().WithStatus(TripStatus.Requested).Build();
        await db.Trips.AddAsync(trip);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync($"/api/v1/trips/{trip.Id}/cancel", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyDb = fixture.GetDbContext();
        var updated = await verifyDb.Trips.FindAsync(trip.Id);
        Assert.NotNull(updated);
        Assert.Equal(TripStatus.Cancelled, updated.Status);
    }

    [Fact]
    public async Task Cancel_WhenTripPickedUp_ReturnsConflictStatusCode()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var trip = new TripBuilder().WithStatus(TripStatus.PickedUp).Build();
        await db.Trips.AddAsync(trip);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync($"/api/v1/trips/{trip.Id}/cancel", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
