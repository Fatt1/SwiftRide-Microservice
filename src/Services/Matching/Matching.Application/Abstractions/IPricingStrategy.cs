using Matching.Application.Dtos;
using Matching.Domain.Entities;

namespace Matching.Application.Abstractions;

public interface IPricingStrategy
{

    PricingBreakdown GetPrice(PricingContext context);
}
