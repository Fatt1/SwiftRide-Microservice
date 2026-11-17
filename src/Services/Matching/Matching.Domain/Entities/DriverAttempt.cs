using MongoDB.Bson.Serialization.Attributes;

namespace Matching.Domain.Entities;

[BsonIgnoreExtraElements]
public class DriverAttempt
{
    public string Outcome { get; set; } = "Offered";
    public Guid DriverId { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}
