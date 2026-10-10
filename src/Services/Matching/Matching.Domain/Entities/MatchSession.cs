using Matching.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Matching.Domain.Entities;

[BsonIgnoreExtraElements]
public class MatchSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public Guid TripId { get; set; }

    [BsonRequired]
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> PickupLocation { get; set; } = default!;

    [BsonIgnore]
    public double PickupLat => PickupLocation?.Coordinates.Latitude ?? 0;

    [BsonIgnore]
    public double PickupLng => PickupLocation?.Coordinates.Longitude ?? 0;

    public double DistanceKm { get; set; }
    public double EstimatedMinutes { get; set; }

    // Sub-document tự động nhúng trọn vẹn vào document cha
    public PricingBreakdown PricingBreakdown { get; set; } = new();

    public Guid? MatchedDriverId { get; set; }

    // Mảng sub-documents
    public List<DriverAttempt> DriverAttempts { get; set; } = [];

    [BsonRepresentation(BsonType.String)]
    public MatchSessionStatus Status { get; set; } = MatchSessionStatus.Searching;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;


    public static GeoJsonPoint<GeoJson2DGeographicCoordinates> CreatePoint(double lat, double lng)
    {
        // QUAN TRỌNG: Thứ tự GeoJSON là Longitude (kinh độ) trước, Latitude (vĩ độ) sau: [lng, lat]
        return GeoJson.Point(GeoJson.Geographic(lng, lat));
    }

    public void SetPickupLocation(double latitude, double longitude)
    {
        PickupLocation = CreatePoint(latitude, longitude);
        LastModifiedAt = DateTime.UtcNow;
    }

    public void AddAttempt(Guid driverId)
    {
        DriverAttempts.Add(new DriverAttempt
        {
            DriverId = driverId,
            AttemptedAt = DateTime.UtcNow
        });
    }

    public void MarkMatched(Guid driverId)
    {
        MatchedDriverId = driverId;
        Status = MatchSessionStatus.Matched;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void MarkNoDriver()
    {
        Status = MatchSessionStatus.NoDriver;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void MarkExpired()
    {
        Status = MatchSessionStatus.Expired;
        LastModifiedAt = DateTime.UtcNow;
    }
}

public class PricingBreakdown
{
    public double DistanceFare { get; set; }

    public double TimeFare { get; set; }

    public double TaxRate { get; set; } = 0.1;

    public double BaseFare { get; set; }

    public double RetentionFactor { get; set; } = 1.0;

    public double FareAfterSurge { get; set; }

    public double FareAfterDiscount { get; set; }

    public double TollFee { get; set; }

    public List<AppliedSurge> AppliedSurges { get; set; } = [];

    public double DiscountAmount { get; set; }


    public double TaxAmount { get; set; }

    public double FinalTotal { get; set; }
}

public class AppliedSurge
{
    [BsonRepresentation(BsonType.String)]
    public SurgeType Type { get; set; }

    public string Name { get; set; } = default!;

    [BsonRepresentation(BsonType.Decimal128)]
    public double Multiplier { get; set; }
}

public class DriverAttempt
{
    public Guid DriverId { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}
