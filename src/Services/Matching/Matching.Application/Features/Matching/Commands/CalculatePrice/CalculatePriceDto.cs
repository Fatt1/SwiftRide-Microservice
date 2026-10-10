using Matching.Domain.Entities;

namespace Matching.Application.Features.Matching.Commands.CalculatePrice;

public class CalculatePriceDto
{
    public double DistanceKm { get; set; }
    public int EstimatedMinutes { get; set; }
    public double DistanceFare { get; set; }
    public double TimeFare { get; set; }
    public double TaxRate { get; set; }
    public double BaseFare { get; set; }
    public double RetentionFactor { get; set; }
    public double FareAfterSurge { get; set; }
    public double FareAfterDiscount { get; set; }
    public double TollFee { get; set; }
    public List<AppliedSurge> AppliedSurges { get; set; } = [];
    public double DiscountAmount { get; set; }
    public double TaxAmount { get; set; }
    public double FinalTotal { get; set; }
}
