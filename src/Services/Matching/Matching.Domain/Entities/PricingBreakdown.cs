using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Matching.Domain.Entities;

public class PricingBreakdown
{
    [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
    public double DistanceFare { get; set; }

    [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
    public double TimeFare { get; set; }

    [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
    public double TaxRate { get; set; } = 0.1;

    [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
    public double BaseFare { get; set; }

    [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
    public double RetentionFactor { get; set; } = 1.0;

    [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
    public double FareAfterSurge { get; set; }

    [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
    public double FareAfterDiscount { get; set; }

    [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
    public double TollFee { get; set; }

    public List<AppliedSurge> AppliedSurges { get; set; } = [];

    [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
    public double DiscountAmount { get; set; }

    [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
    public double TaxAmount { get; set; }

    [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
    public double FinalTotal { get; set; }
}
