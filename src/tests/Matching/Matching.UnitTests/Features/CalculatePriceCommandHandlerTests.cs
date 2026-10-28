using Matching.Application.Configurations;
using Matching.Application.Features.Matching.Commands.CalculatePrice;
using Matching.Domain.Entities;
using Matching.Domain.Repositories;
using Matching.UnitTests.Common.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Matching.UnitTests.Features;

public class CalculatePriceCommandHandlerTests
{
    private readonly IMatchingRepository _matchingRepository = Substitute.For<IMatchingRepository>();
    private readonly ILogger<CalculatePriceCommandHandler> _logger = Substitute.For<ILogger<CalculatePriceCommandHandler>>();

    [Fact]
    public async Task Handle_WithValidCommand_CalculatesPricingAndSavesMatchSession()
    {
        // Arrange
        var pricingConfig = new PricingConfigBuilder()
            .WithRates(perKmRate: 8000, perMinRate: 500)
            .WithTollFee(10000)
            .WithTaxRate(0.10)
            .Build();
        var options = Options.Create(pricingConfig);
        var handler = new CalculatePriceCommandHandler(_matchingRepository, _logger, options);

        var tripId = Guid.NewGuid();
        // Coordinates for roughly 3.5 km trip in HCMC
        var command = new CalculatePriceCommand(
            IsRaining: false,
            PromoCode: null,
            HasToll: false,
            PickupLongitude: 106.6980,
            PickupLatitude: 10.7725,
            DestinationLongitude: 106.7218,
            DestinationLatitude: 10.7950,
            TripId: tripId
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.NotNull(dto);
        Assert.True(dto.DistanceKm > 0);
        Assert.True(dto.EstimatedMinutes > 0);
        Assert.True(dto.FinalTotal > 0);
        Assert.Equal(0.10, dto.TaxRate);

        // Verify session was persisted in repository
        await _matchingRepository.Received(1).CreateAsync(
            Arg.Is<MatchSession>(s =>
                s.TripId == tripId &&
                s.DistanceKm == dto.DistanceKm &&
                s.PricingBreakdown.FinalTotal == dto.FinalTotal),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPromoCodeAndToll_AppliesModifiersToCalculatedTotal()
    {
        // Arrange
        var pricingConfig = new PricingConfigBuilder()
            .WithRates(perKmRate: 8000, perMinRate: 500)
            .WithTollFee(10000)
            .WithTaxRate(0.10)
            .WithPromoCode("DISCOUNT20", factor: 0.8)
            .Build();
        var options = Options.Create(pricingConfig);
        var handler = new CalculatePriceCommandHandler(_matchingRepository, _logger, options);

        var tripId = Guid.NewGuid();
        var command = new CalculatePriceCommand(
            IsRaining: false,
            PromoCode: "DISCOUNT20",
            HasToll: true,
            PickupLongitude: 106.6980,
            PickupLatitude: 10.7725,
            DestinationLongitude: 106.7218,
            DestinationLatitude: 10.7950,
            TripId: tripId
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal(10000, dto.TollFee);
        Assert.Equal(0.8, dto.RetentionFactor);
        Assert.True(dto.DiscountAmount > 0);
    }
}
