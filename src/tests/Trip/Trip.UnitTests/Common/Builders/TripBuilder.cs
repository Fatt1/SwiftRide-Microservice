using global::Trip.Domain.Entities;
using global::Trip.Domain.Enums;

namespace Trip.UnitTests.Common.Builders;

public class TripBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _riderId = Guid.NewGuid();
    private Guid? _driverId;
    private string _pickupAddress = "227 Nguyen Van Cu, Ward 4, District 5, HCMC";
    private string _dropoffAddress = "Ben Thanh Market, District 1, HCMC";
    private double _pickupLat = 10.762622;
    private double _pickupLng = 106.682373;
    private double _dropoffLat = 10.772545;
    private double _dropoffLng = 106.698032;
    private decimal? _estimatedFare = 50000m;
    private decimal? _finalFare;
    private decimal? _distanceKm = 5.0m;
    private string? _quoteId = Guid.NewGuid().ToString();
    private TripStatus _status = TripStatus.Requested;
    private string? _paymentFailureReason;

    public TripBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public TripBuilder WithRiderId(Guid riderId)
    {
        _riderId = riderId;
        return this;
    }

    public TripBuilder WithDriverId(Guid driverId)
    {
        _driverId = driverId;
        return this;
    }

    public TripBuilder WithPickupAddress(string pickupAddress)
    {
        _pickupAddress = pickupAddress;
        return this;
    }

    public TripBuilder WithDropoffAddress(string dropoffAddress)
    {
        _dropoffAddress = dropoffAddress;
        return this;
    }

    public TripBuilder WithPickupLocation(double lat, double lng)
    {
        _pickupLat = lat;
        _pickupLng = lng;
        return this;
    }

    public TripBuilder WithDropoffLocation(double lat, double lng)
    {
        _dropoffLat = lat;
        _dropoffLng = lng;
        return this;
    }

    public TripBuilder WithEstimatedFare(decimal fare)
    {
        _estimatedFare = fare;
        return this;
    }

    public TripBuilder WithFinalFare(decimal fare)
    {
        _finalFare = fare;
        return this;
    }

    public TripBuilder WithDistanceKm(decimal distanceKm)
    {
        _distanceKm = distanceKm;
        return this;
    }

    public TripBuilder WithQuoteId(string quoteId)
    {
        _quoteId = quoteId;
        return this;
    }

    public TripBuilder WithPaymentFailureReason(string reason)
    {
        _paymentFailureReason = reason;
        return this;
    }

    public TripBuilder WithStatus(TripStatus status)
    {
        _status = status;
        return this;
    }

    public TripEntity Build()
    {
        var trip = TripEntity.Create(
            _riderId,
            _pickupAddress,
            _dropoffAddress,
            _pickupLat,
            _pickupLng,
            _dropoffLat,
            _dropoffLng);

        trip.Id = _id;

        if (_estimatedFare.HasValue && _distanceKm.HasValue)
        {
            trip.SetPricingInfo(_estimatedFare.Value, _distanceKm.Value, _quoteId ?? Guid.NewGuid().ToString());
        }

        var driverId = _driverId ?? Guid.NewGuid();

        switch (_status)
        {
            case TripStatus.Requested:
                break;
            case TripStatus.DriverAccepted:
                trip.AcceptByDriver(driverId);
                break;
            case TripStatus.DriverRejected:
                trip.RejectByDriver();
                break;
            case TripStatus.PickedUp:
                trip.AcceptByDriver(driverId);
                trip.MarkPickedUp();
                break;
            case TripStatus.DroppedOff:
                trip.AcceptByDriver(driverId);
                trip.MarkPickedUp();
                trip.MarkDroppedOff(_finalFare ?? _estimatedFare ?? 50000m);
                break;
            case TripStatus.Completed:
                trip.AcceptByDriver(driverId);
                trip.MarkPickedUp();
                trip.MarkDroppedOff(_finalFare ?? _estimatedFare ?? 50000m);
                trip.MarkCompleted();
                break;
            case TripStatus.PaymentFailed:
                trip.AcceptByDriver(driverId);
                trip.MarkPickedUp();
                trip.MarkDroppedOff(_finalFare ?? _estimatedFare ?? 50000m);
                trip.MarkPaymentFailed(_paymentFailureReason ?? "Payment declined by provider");
                break;
            case TripStatus.Cancelled:
                trip.Cancel();
                break;
        }

        return trip;
    }
}
