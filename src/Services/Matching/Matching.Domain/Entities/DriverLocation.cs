using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Matching.Domain.Entities;

public class DriverLocation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonRequired]
    public Guid DriverId { get; set; }

    [BsonRequired]
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; } = default!;

    public bool IsAvailable { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DriverLocation() { }

    public static DriverLocation Create(Guid driverId, double latitude, double longitude, bool isAvailable = true)
    {
        return new DriverLocation
        {
            DriverId = driverId,
            IsAvailable = isAvailable,
            Location = CreatePoint(latitude, longitude),
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateLocation(double latitude, double longitude)
    {
        Location = CreatePoint(latitude, longitude);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLocation(GeoJsonPoint<GeoJson2DGeographicCoordinates> location)
    {
        Location = location;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        UpdatedAt = DateTime.UtcNow;
    }

    // Helper tạo nhanh từ Lat/Lng theo chuẩn GeoJSON [Longitude, Latitude]
    public static GeoJsonPoint<GeoJson2DGeographicCoordinates> CreatePoint(double lat, double lng)
    {
        return GeoJson.Point(GeoJson.Geographic(lng, lat));
    }

    [BsonIgnore]
    public double Latitude => Location?.Coordinates.Latitude ?? 0;

    [BsonIgnore]
    public double Longitude => Location?.Coordinates.Longitude ?? 0;
}
