using Matching.Domain.Entities;
using MongoDB.Bson;

namespace Matching.IntegrationTests.Common.Builders;

public class DriverLocationBuilder
{
    private string _id = ObjectId.GenerateNewId().ToString();
    private Guid _driverId = Guid.NewGuid();
    private string _fullName = "Nguyen Van Driver";
    private double _latitude = 10.7769;
    private double _longitude = 106.7009;
    private bool _isAvailable = true;
    private DateTime _updatedAt = DateTime.UtcNow;

    public DriverLocationBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public DriverLocationBuilder WithDriverId(Guid driverId)
    {
        _driverId = driverId;
        return this;
    }

    public DriverLocationBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public DriverLocationBuilder WithLocation(double latitude, double longitude)
    {
        _latitude = latitude;
        _longitude = longitude;
        return this;
    }

    public DriverLocationBuilder WithAvailability(bool isAvailable)
    {
        _isAvailable = isAvailable;
        return this;
    }

    public DriverLocationBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public DriverLocation Build()
    {
        return new DriverLocation
        {
            Id = _id,
            DriverId = _driverId,
            FullName = _fullName,
            Location = DriverLocation.CreatePoint(_latitude, _longitude),
            IsAvailable = _isAvailable,
            UpdatedAt = _updatedAt
        };
    }
}
