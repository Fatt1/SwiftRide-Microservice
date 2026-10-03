using Contracts.Domain;
using Matching.Domain.Enums;

namespace Matching.Domain.Entities;

public class MatchSession : EntityAuditableBase<Guid>
{
    public Guid TripId { get; set; }
    public Guid RiderId { get; set; }
    public double PickupLat { get; set; }
    public double PickupLng { get; set; }
    public double DistanceKm { get; set; }
    public double EstimatedMinutes { get; set; }
    public PricingBreakdown PricingBreakdown { get; set; } = new();
    public Guid? MatchedDriverId { get; set; }
    public List<DriverAttempt> DriverAttempts { get; set; } = [];
    public MatchSessionStatus Status { get; set; } = MatchSessionStatus.Searching;

    public MatchSession() { }

    public void AddAttempt(Guid driverId, DriverResponse response)
    {
        DriverAttempts.Add(new DriverAttempt
        {
            DriverId = driverId,
            Response = response,
            AttemptedAt = DateTimeOffset.UtcNow
        });
    }

    public void MarkMatched(Guid driverId)
    {
        MatchedDriverId = driverId;
        Status = MatchSessionStatus.Matched;
    }

    public void MarkNoDriver()
    {
        Status = MatchSessionStatus.NoDriver;
    }

    public void MarkExpired()
    {
        Status = MatchSessionStatus.Expired;
    }
}

public class PricingBreakdown
{
    public decimal BaseFare { get; set; }
    public decimal DistanceFare { get; set; }
    public decimal TimeFare { get; set; }
    public decimal Subtotal { get; set; }
    public List<AppliedSurge> AppliedSurges { get; set; } = [];
    public decimal AfterSurge { get; set; }
    public string? PromoCode { get; set; }
    public decimal PromoFactor { get; set; } = 1.0m;
    public decimal AfterPromo { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalFare { get; set; }

}

public class AppliedSurge
{
    public SurgeType Type { get; set; }
    public string Name { get; set; } = default!;
    public decimal Multiplier { get; set; }
}

public class DriverAttempt
{
    public Guid DriverId { get; set; }
    public DriverResponse Response { get; set; }
    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;
}
