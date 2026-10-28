using Matching.Domain.Entities;
using Matching.Domain.Enums;

namespace Matching.UnitTests.Common.Builders;

public class MatchSessionBuilder
{
    private string _id = "507f1f77bcf86cd799439012";
    private Guid _tripId = Guid.NewGuid();
    private double _pickupLat = 10.7769;
    private double _pickupLng = 106.7009;
    private double _distanceKm = 5.0;
    private double _estimatedMinutes = 15.0;
    private MatchSessionStatus _status = MatchSessionStatus.Searching;
    private Guid? _matchedDriverId;
    private readonly List<DriverAttempt> _driverAttempts = [];
    private PricingBreakdown _pricingBreakdown = new()
    {
        BaseFare = 40000,
        DistanceFare = 40000,
        TimeFare = 7500,
        FinalTotal = 50000,
        TaxRate = 0.1,
        TaxAmount = 5000
    };

    public MatchSessionBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public MatchSessionBuilder WithTripId(Guid tripId)
    {
        _tripId = tripId;
        return this;
    }

    public MatchSessionBuilder WithPickupLocation(double lat, double lng)
    {
        _pickupLat = lat;
        _pickupLng = lng;
        return this;
    }

    public MatchSessionBuilder WithDistance(double distanceKm, double estimatedMinutes)
    {
        _distanceKm = distanceKm;
        _estimatedMinutes = estimatedMinutes;
        return this;
    }

    public MatchSessionBuilder WithStatus(MatchSessionStatus status)
    {
        _status = status;
        return this;
    }

    public MatchSessionBuilder WithMatchedDriverId(Guid driverId)
    {
        _matchedDriverId = driverId;
        return this;
    }

    public MatchSessionBuilder WithAttempts(params Guid[] driverIds)
    {
        _driverAttempts.Clear();
        foreach (var id in driverIds)
        {
            _driverAttempts.Add(new DriverAttempt
            {
                DriverId = id,
                AttemptedAt = DateTime.UtcNow
            });
        }
        return this;
    }

    public MatchSessionBuilder WithPricingBreakdown(PricingBreakdown pricingBreakdown)
    {
        _pricingBreakdown = pricingBreakdown;
        return this;
    }

    public MatchSession Build()
    {
        var session = new MatchSession
        {
            Id = _id,
            TripId = _tripId,
            PickupLocation = MatchSession.CreatePoint(_pickupLat, _pickupLng),
            DistanceKm = _distanceKm,
            EstimatedMinutes = _estimatedMinutes,
            Status = _status,
            MatchedDriverId = _matchedDriverId,
            DriverAttempts = [.. _driverAttempts],
            PricingBreakdown = _pricingBreakdown,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };
        return session;
    }
}
