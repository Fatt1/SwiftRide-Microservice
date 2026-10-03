using Contracts.Domain;

namespace Matching.Domain.Entities;

public class DriverLocation : EntityBase<Guid>
{
    public Guid DriverId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsAvailable { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DriverLocation() { }

    public static DriverLocation Create(Guid driverId, double latitude, double longitude, bool isAvailable = true)
    {
        return new DriverLocation
        {
            Id = Guid.CreateVersion7(),
            DriverId = driverId,
            Latitude = latitude,
            Longitude = longitude,
            IsAvailable = isAvailable,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
