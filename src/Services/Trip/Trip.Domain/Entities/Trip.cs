using Contracts.Domain;
using Trip.Domain.Enums;

namespace Trip.Domain.Entities;

public class Trip : EntityAuditableBase<Guid>
{
    public Guid RiderId { get; private set; }
    public Guid? DriverId { get; private set; }

    public TripStatus Status { get; private set; }

    public string PickupAddress { get; private set; } = default!;
    public string DropoffAddress { get; private set; } = default!;

    public double PickupLat { get; private set; }
    public double PickupLng { get; private set; }
    public double DropoffLat { get; private set; }
    public double DropoffLng { get; private set; }

    public decimal? EstimatedFare { get; private set; }
    public decimal? FinalFare { get; private set; }
    public decimal? DistanceKm { get; private set; }

    /// <summary>Lý do thất bại khi thanh toán</summary>
    public string? PaymentFailureReason { get; private set; }

    /// <summary>Reference to match_sessions in MongoDB</summary>
    public string? QuoteId { get; private set; }

    /// <summary>Saga / idempotency key</summary>
    public Guid CorrelationId { get; private set; }

    // EF Core
    private Trip() { }

    public static Trip Create(
        Guid riderId,
        string pickupAddress,
        string dropoffAddress,
        double pickupLat,
        double pickupLng,
        double dropoffLat,
        double dropoffLng)
    {
        return new Trip
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
            CorrelationId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow,
        };
    }

    public void SetPricingInfo(decimal estimatedFare, decimal distanceKm, string quoteId)
    {
        EstimatedFare = estimatedFare;
        DistanceKm = distanceKm;
        QuoteId = quoteId;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void AcceptByDriver(Guid driverId)
    {
        DriverId = driverId;
        Status = TripStatus.DriverAccepted;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPickedUp()
    {
        Status = TripStatus.PickedUp;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDroppedOff(decimal finalFare)
    {
        FinalFare = finalFare;
        Status = TripStatus.DroppedOff;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = TripStatus.Completed;
        PaymentFailureReason = null;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPaymentFailed(string reason)
    {
        Status = TripStatus.PaymentFailed;
        PaymentFailureReason = reason;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        Status = TripStatus.Cancelled;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }
}
