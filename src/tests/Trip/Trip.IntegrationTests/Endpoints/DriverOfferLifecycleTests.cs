using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Trip.API.DTOs;
using Trip.Domain.Enums;
using Trip.IntegrationTests.Common.Builders;
using Trip.IntegrationTests.Fixtures;
using Xunit;
namespace Trip.IntegrationTests.Endpoints;
public class DriverOfferLifecycleTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Reject_KEqualsOne_CountsDuplicateOnceAndFailsOnSecondDriver()
    {
        // Arrange
        await fixture.ResetDatabaseAsync(); using var client = fixture.CreateClient(); using var db = fixture.GetDbContext();
        var trip = new TripBuilder().Build(); var firstDriver = Guid.NewGuid();
        trip.SetDriverOffer(trip.MatchSessionId!, firstDriver, 1);
        db.Trips.Add(trip); await db.SaveChangesAsync();
        // Act
        var first = await client.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/driver-reject", new DriverRejectRequest(firstDriver));
        var duplicate = await client.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/driver-reject", new DriverRejectRequest(firstDriver));
        await db.Entry(trip).ReloadAsync();
        // Assert
        Assert.Equal(HttpStatusCode.OK, first.StatusCode); Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var response = await first.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(1, response.GetProperty("rejectCount").GetInt32());
        Assert.Equal(1, response.GetProperty("maxAllowedRejections").GetInt32());
        Assert.False(response.TryGetProperty("count", out _));
        Assert.Equal(1, trip.RejectCount); Assert.Equal(TripStatus.Requested, trip.Status); Assert.Null(trip.OfferedDriverId);
        // Arrange: a new manual matching result has arrived.
        var secondDriver = Guid.NewGuid();
        trip.SetDriverOffer(trip.MatchSessionId!, secondDriver, 1); await db.SaveChangesAsync();
        // Act
        var staleAccept = await client.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/driver-accept", new DriverAcceptRequest(firstDriver));
        var second = await client.PostAsJsonAsync($"/api/v1/trips/{trip.Id}/driver-reject", new DriverRejectRequest(secondDriver));
        await db.Entry(trip).ReloadAsync();
        // Assert
        Assert.Equal(HttpStatusCode.Conflict, staleAccept.StatusCode); Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(2, trip.RejectCount); Assert.Equal(TripStatus.Failed, trip.Status); Assert.NotNull(trip.FailureReason);
    }
    [Fact]
    public async Task Schema_TripDoesNotHaveApplicationVersionColumn()
    {
        // Arrange
        using var db = fixture.GetDbContext();
        // Act
        var columns = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Trips') AND name = N'Version'").SingleAsync();
        // Assert
        Assert.Equal(0, columns);
    }
}
