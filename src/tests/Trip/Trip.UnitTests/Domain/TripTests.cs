using Trip.Domain.Enums;
using Trip.UnitTests.Common.Builders;
using Xunit;
namespace Trip.UnitTests.Domain;
public class TripTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(3)] [InlineData(7)]
    public void RejectByDriver_ConfigurableLimit_FailsOnlyAboveK(int k)
    {
        // Arrange
        var trip = new TripBuilder().Build();
        // Act / Assert: observe every rejection boundary.
        for (var count = 1; count <= k + 1; count++)
        {
            var driver = Guid.NewGuid();
            Assert.True(trip.SetDriverOffer(trip.MatchSessionId!, driver, k));
            trip.RejectByDriver(driver);
            Assert.Throws<InvalidOperationException>(() => trip.RejectByDriver(driver));
            Assert.Equal(count, trip.RejectCount);
            Assert.Null(trip.OfferedDriverId);
            Assert.Equal(count <= k ? TripStatus.Requested : TripStatus.Failed, trip.Status);
        }
        Assert.NotNull(trip.FailureReason);
    }
    [Fact]
    public void AcceptByDriver_StaleOrWrongDriver_CannotTakeTrip()
    {
        // Arrange
        var trip = new TripBuilder().Build(); var a = Guid.NewGuid(); var b = Guid.NewGuid();

        trip.SetDriverOffer(trip.MatchSessionId!, a, 3);
        trip.RejectByDriver(a);
        trip.SetDriverOffer(trip.MatchSessionId!, b, 3);
        // Act
        Assert.Throws<InvalidOperationException>(() => trip.AcceptByDriver(a));
        trip.AcceptByDriver(b);
        // Assert
        Assert.Equal(b, trip.DriverId); Assert.Equal(TripStatus.DriverAccepted, trip.Status);
        Assert.False(trip.SetDriverOffer(trip.MatchSessionId!, a, 3));
    }
    [Fact]
    public void Lifecycle_InvalidTransitions_AreRejected()
    {
        // Arrange
        var trip = new TripBuilder().Build();
        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => trip.MarkPickedUp());
        Assert.Throws<InvalidOperationException>(() => trip.MarkCompleted());
        Assert.Throws<InvalidOperationException>(() => trip.MarkDroppedOff());
        trip.Cancel();
        Assert.Throws<InvalidOperationException>(() => trip.MarkFailed("late failure"));
    }
    [Fact]
    public void Payment_Pending_CannotStartAnotherPaymentOrCancel()
    {
        // Arrange
        var trip = new TripBuilder().WithStatus(TripStatus.PaymentPending).Build();
        // Act
        // Assert
        Assert.Equal(TripStatus.PaymentPending, trip.Status);
        Assert.Throws<InvalidOperationException>(() => trip.BeginPayment());
        Assert.Throws<InvalidOperationException>(() => trip.Cancel());
    }
    [Fact]
    public void SetDriverOffer_DuplicateAndDifferentOffer_KeepCurrentOffer()
    {
        // Arrange
        var trip = new TripBuilder().Build(); var driver = Guid.NewGuid();
        // Act
        Assert.True(trip.SetDriverOffer(trip.MatchSessionId!, driver, 1));
        Assert.True(trip.SetDriverOffer(trip.MatchSessionId!, driver, 1));
        Assert.False(trip.SetDriverOffer(trip.MatchSessionId!, Guid.NewGuid(), 1));
        // Assert
        Assert.Equal(driver, trip.OfferedDriverId); Assert.Null(trip.DriverId);
    }
}
