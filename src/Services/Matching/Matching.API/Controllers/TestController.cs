using Matching.Application.Configurations;
using Matching.Application.Dtos;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Infrastructure.Pricing.Stategies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Matching.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    private readonly PricingConfig _config;
    public TestController(IOptions<PricingConfig> options)
    {
        _config = options.Value;
    }


    [HttpGet("/")]
    public ActionResult<PricingBreakdown> GetPrice()
    {
        var context = new PricingContext(
            DistanceKm: 10.0m,
            EstimatedMinutes: 15,
            OrderTime: DateTime.UtcNow,
            Weather: WeatherCondition.Rain,
            HasToll: true,
            PromoCode: "SWIFTRIDE10"
            );

        var standardPricing = new StandardPricing(_config);

        var timePricing = new TimeSurgePricing(standardPricing, _config);
        var weatherSurgePricing = new WeatherSurgePricing(timePricing, _config);
        var tollPricing = new TollPricing(weatherSurgePricing, _config);
        var promoPricing = new PromotionPricing(tollPricing, _config);



        var breakdown = promoPricing.GetPrice(context);

        var taxAmount = breakdown.FinalTotal * _config.TaxRate;
        breakdown.FinalTotal = Math.Round(breakdown.FinalTotal + taxAmount, 2);
        breakdown.TaxAmount = taxAmount;
        breakdown.TaxRate = _config.TaxRate;
        return Ok(breakdown);
    }
}
