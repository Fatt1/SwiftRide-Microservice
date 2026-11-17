using Matching.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Matching.Domain.Entities;

public class AppliedSurge
{
    [BsonRepresentation(BsonType.String)]
    public SurgeType Type { get; set; }

    public string Name { get; set; } = default!;

    [BsonRepresentation(BsonType.Decimal128)]
    public double Multiplier { get; set; }
}
