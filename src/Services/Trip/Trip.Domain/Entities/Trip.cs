using Contracts.Domain;
using Trip.Domain.Enums;
namespace Trip.Domain.Entities;

public class Trip : EntityAuditableBase<Guid>
{
    public Guid RiderId { get; private set; }
    public Guid? DriverId { get; private set; }
    public TripStatus Status { get; private set; }
    public string PickupAddress { get; private set; } = "";
    public string DropoffAddress { get; private set; } = "";
    public double PickupLat { get; private set; }
    public double PickupLng { get; private set; }
    public double DropoffLat { get; private set; }
    public double DropoffLng { get; private set; }
    public decimal? EstimatedFare { get; private set; }
    public decimal? DistanceKm { get; private set; }
    public int EstimatedMinutes { get; private set; }
    public string? MatchSessionId { get; private set; }
    public Guid? OfferedDriverId { get; private set; }
    public int MaxAllowedRejections { get; private set; }
    public int RejectCount { get; private set; }
    public string? FailureReason { get; private set; }
    // Kept as a read-only compatibility alias for existing callers.
    public string? QuoteId => MatchSessionId;
    private Trip() { }
    public static Trip Create(Guid riderId, string pickupAddress, string dropoffAddress,
        double pickupLat, double pickupLng, double dropoffLat, double dropoffLng) => new()
        {
            Id = Guid.NewGuid(),
            RiderId = riderId,
            Status = TripStatus.Requested,
            PickupAddress = pickupAddress,
            DropoffAddress = dropoffAddress,
            PickupLat = pickupLat,
            PickupLng = pickupLng,
            DropoffLat = dropoffLat,
            DropoffLng = dropoffLng,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow
        };
    private void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
    private void Touch() => LastModifiedAt = DateTimeOffset.UtcNow;
    public void SetPricingInfo(decimal estimatedFare, decimal distanceKm, string quoteId, int estimatedMinutes = 0)
    {
        Require(Status == TripStatus.Requested && OfferedDriverId == null, "Pricing is already locked.");
        Require(estimatedFare > 0 && distanceKm >= 0 && !string.IsNullOrWhiteSpace(quoteId), "Invalid quote.");
        EstimatedFare = estimatedFare;
        DistanceKm = distanceKm;
        MatchSessionId = quoteId;
        EstimatedMinutes = estimatedMinutes;
        Touch();
    }
    public bool SetDriverOffer(string sessionId, Guid driverId, int maxRejections)
    {
        if (Status != TripStatus.Requested || sessionId != MatchSessionId)
            return false;

        OfferedDriverId = driverId;
        MaxAllowedRejections = maxRejections;
        Touch();
        return true;
    }
    public bool IsOfferedDriver(Guid driverId) => driverId != Guid.Empty &&
        OfferedDriverId == driverId;
    public void AcceptByDriver(Guid driverId)
    {
        Require(Status == TripStatus.Requested && IsOfferedDriver(driverId), "The driver offer is no longer active.");
        DriverId = driverId;
        Status = TripStatus.DriverAccepted;
        Touch();
    }
    public void RejectByDriver(Guid driverId)
    {
        Require(Status == TripStatus.Requested && IsOfferedDriver(driverId), "The driver offer is no longer active.");
        OfferedDriverId = null;
        RejectCount++;
        if (RejectCount > MaxAllowedRejections)
            MarkFailed("Driver rejection limit exceeded.");
        Touch();
    }
    public void MarkPickedUp()
    {
        Require(Status == TripStatus.DriverAccepted, "Driver must accept first.");
        Status = TripStatus.PickedUp;
        Touch();
    }
    public void MarkDroppedOff()
    {
        Require(Status == TripStatus.PickedUp, "Invalid dropoff.");
        Status = TripStatus.DroppedOff;
        Touch();
    }
    public void BeginPayment()
    {
        Require(Status == TripStatus.DroppedOff, "Payment can only start after dropoff.");
        Status = TripStatus.PaymentPending;
        Touch();
    }
    public void MarkCompleted()
    {
        Require(Status == TripStatus.PaymentPending, "Payment must be pending.");
        Status = TripStatus.Completed;
        Touch();
    }
    public void MarkFailed(string reason)
    {
        Require(Status == TripStatus.Requested, "Only an unassigned trip can fail matching.");
        Status = TripStatus.Failed;
        FailureReason = reason;
        OfferedDriverId = null;
        Touch();
    }
    public void Cancel()
    {
        Require(Status is TripStatus.Requested or TripStatus.DriverAccepted, "Trip cannot be cancelled.");
        Status = TripStatus.Cancelled;
        OfferedDriverId = null;
        Touch();
    }
}
