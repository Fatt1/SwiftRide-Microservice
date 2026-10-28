using Trip.Domain.Enums;
using Trip.UnitTests.Common.Builders;
using Xunit;

namespace Trip.UnitTests.Domain;

public class TripTests
{
    [Fact]
    public void Create_WithValidParameters_InitializesTripCorrectly()
    {
        // Arrange
        var riderId = Guid.NewGuid();
        const string pickup = "227 Nguyen Van Cu, D5, HCMC";
        const string dropoff = "Ben Thanh Market, D1, HCMC";
        const double pickupLat = 10.762622;
        const double pickupLng = 106.682373;
        const double dropoffLat = 10.772545;
        const double dropoffLng = 106.698032;

        // Act
        var trip = TripEntity.Create(
            riderId, pickup, dropoff, pickupLat, pickupLng, dropoffLat, dropoffLng);

        // Assert
        Assert.NotEqual(Guid.Empty, trip.Id);
        Assert.Equal(riderId, trip.RiderId);
        Assert.Null(trip.DriverId);
        Assert.Equal(TripStatus.Requested, trip.Status);
        Assert.Equal(pickup, trip.PickupAddress);
        Assert.Equal(dropoff, trip.DropoffAddress);
        Assert.Equal(pickupLat, trip.PickupLat);
        Assert.Equal(pickupLng, trip.PickupLng);
        Assert.Equal(dropoffLat, trip.DropoffLat);
        Assert.Equal(dropoffLng, trip.DropoffLng);
        Assert.NotEqual(Guid.Empty, trip.CorrelationId);
        Assert.True(trip.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(trip.LastModifiedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void SetPricingInfo_WhenCalled_UpdatesEstimatedFareDistanceAndQuoteId()
    {
        // Arrange
        var trip = new TripBuilder().Build();
        const decimal estimatedFare = 75000m;
        const decimal distanceKm = 8.5m;
        const string quoteId = "quote-12345";
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        trip.SetPricingInfo(estimatedFare, distanceKm, quoteId);

        // Assert
        Assert.Equal(estimatedFare, trip.EstimatedFare);
        Assert.Equal(distanceKm, trip.DistanceKm);
        Assert.Equal(quoteId, trip.QuoteId);
        Assert.True(trip.LastModifiedAt >= beforeUpdate);
    }

    [Fact]
    public void AcceptByDriver_WhenCalled_SetsDriverIdAndStatusDriverAccepted()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.Requested).Build();
        var driverId = Guid.NewGuid();
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        trip.AcceptByDriver(driverId);

        // Assert
        Assert.Equal(driverId, trip.DriverId);
        Assert.Equal(TripStatus.DriverAccepted, trip.Status);
        Assert.True(trip.LastModifiedAt >= beforeUpdate);
    }

    [Fact]
    public void MarkPickedUp_WhenCalled_UpdatesStatusToPickedUp()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.DriverAccepted).Build();
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        trip.MarkPickedUp();

        // Assert
        Assert.Equal(TripStatus.PickedUp, trip.Status);
        Assert.True(trip.LastModifiedAt >= beforeUpdate);
    }

    [Fact]
    public void MarkDroppedOff_WhenCalled_SetsFinalFareAndStatusDroppedOff()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.PickedUp).Build();
        const decimal finalFare = 82000m;
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        trip.MarkDroppedOff(finalFare);

        // Assert
        Assert.Equal(TripStatus.DroppedOff, trip.Status);
        Assert.Equal(finalFare, trip.FinalFare);
        Assert.True(trip.LastModifiedAt >= beforeUpdate);
    }

    [Fact]
    public void MarkCompleted_WhenCalled_SetsStatusCompletedAndClearsPaymentFailureReason()
    {
        // Arrange
        var trip = new TripBuilder()
            .WithStatus(TripStatus.PaymentFailed)
            .WithPaymentFailureReason("Card declined")
            .Build();
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        trip.MarkCompleted();

        // Assert
        Assert.Equal(TripStatus.Completed, trip.Status);
        Assert.Null(trip.PaymentFailureReason);
        Assert.True(trip.LastModifiedAt >= beforeUpdate);
    }

    [Fact]
    public void MarkPaymentFailed_WhenCalled_SetsStatusPaymentFailedAndReason()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.DroppedOff).Build();
        const string reason = "Insufficient funds in rider wallet";
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        trip.MarkPaymentFailed(reason);

        // Assert
        Assert.Equal(TripStatus.PaymentFailed, trip.Status);
        Assert.Equal(reason, trip.PaymentFailureReason);
        Assert.True(trip.LastModifiedAt >= beforeUpdate);
    }

    [Fact]
    public void RejectByDriver_WhenCalled_SetsStatusDriverRejected()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.Requested).Build();
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        trip.RejectByDriver();

        // Assert
        Assert.Equal(TripStatus.DriverRejected, trip.Status);
        Assert.True(trip.LastModifiedAt >= beforeUpdate);
    }

    [Fact]
    public void Cancel_WhenCalled_SetsStatusCancelled()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.Requested).Build();
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        trip.Cancel();

        // Assert
        Assert.Equal(TripStatus.Cancelled, trip.Status);
        Assert.True(trip.LastModifiedAt >= beforeUpdate);
    }
}
