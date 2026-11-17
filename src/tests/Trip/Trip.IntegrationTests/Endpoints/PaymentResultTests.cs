using EventBus.Messages.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Trip.Domain.Enums;
using Trip.Infrastructure.Consumers;
using Trip.Infrastructure.Repositories;
using Trip.IntegrationTests.Common.Builders;
using Trip.IntegrationTests.Fixtures;
using Xunit;

namespace Trip.IntegrationTests.Endpoints;

public class PaymentResultTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Consume_FailureThenSuccessThenReplayedResults_CompletesTripOnlyOnce()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var trip = new TripBuilder().WithStatus(TripStatus.PaymentPending).Build();
        db.Trips.Add(trip);
        await db.SaveChangesAsync();
        var repository = new TripRepository(db);
        var failedConsumer = new PaymentFailedConsumer(repository, NullLogger<PaymentFailedConsumer>.Instance);
        var completedConsumer = new PaymentCompletedConsumer(repository, NullLogger<PaymentCompletedConsumer>.Instance);
        var failure = Substitute.For<ConsumeContext<PaymentFailedEvent>>();
        failure.Message.Returns(new PaymentFailedEvent
        {
            TripId = trip.Id, RiderId = trip.RiderId, Amount = trip.EstimatedFare!.Value,
            CorrelationId = Guid.NewGuid(), Reason = "Previous attempt failed"
        });
        var success = Substitute.For<ConsumeContext<PaymentCompletedEvent>>();
        success.Message.Returns(new PaymentCompletedEvent
        {
            TripId = trip.Id, RiderId = trip.RiderId, DriverId = trip.DriverId!.Value,
            Amount = trip.EstimatedFare.Value, PaymentId = Guid.NewGuid(), CorrelationId = Guid.NewGuid()
        });

        // Act
        await failedConsumer.Consume(failure);
        db.ChangeTracker.Clear();
        trip = await db.Trips.AsNoTracking().SingleAsync(t => t.Id == trip.Id);

        // Assert
        Assert.Equal(TripStatus.PaymentPending, trip.Status);
        Assert.Null(trip.FailureReason);

        // Act
        await completedConsumer.Consume(success);
        await failedConsumer.Consume(failure);
        await completedConsumer.Consume(success);
        db.ChangeTracker.Clear();
        trip = await db.Trips.AsNoTracking().SingleAsync(t => t.Id == trip.Id);

        // Assert
        Assert.Equal(TripStatus.Completed, trip.Status);
        Assert.Null(trip.FailureReason);
        Assert.Null(db.Model.FindEntityType(typeof(global::Trip.Domain.Entities.Trip))!.FindProperty("PaymentRequestId"));
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Trips' AND COLUMN_NAME = 'PaymentRequestId'";
        Assert.Equal(0, (int)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task Consume_SuccessBeforeDropoff_KeepsTripPickedUp()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var db = fixture.GetDbContext();
        var trip = new TripBuilder().WithStatus(TripStatus.PickedUp).Build();
        db.Trips.Add(trip);
        await db.SaveChangesAsync();
        var context = Substitute.For<ConsumeContext<PaymentCompletedEvent>>();
        context.Message.Returns(new PaymentCompletedEvent
        {
            TripId = trip.Id, RiderId = trip.RiderId, DriverId = trip.DriverId!.Value,
            Amount = trip.EstimatedFare!.Value, PaymentId = Guid.NewGuid()
        });
        var consumer = new PaymentCompletedConsumer(new TripRepository(db), NullLogger<PaymentCompletedConsumer>.Instance);

        // Act
        await consumer.Consume(context);
        db.ChangeTracker.Clear();
        trip = await db.Trips.AsNoTracking().SingleAsync(t => t.Id == trip.Id);

        // Assert
        Assert.Equal(TripStatus.PickedUp, trip.Status);
    }
}
