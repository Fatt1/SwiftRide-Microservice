using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared;
using Trip.Application.Common.Interfaces;
using Trip.Application.DTOs;
using Trip.Application.Features.Trips.Commands.CreateTrip;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
using Xunit;

namespace Trip.UnitTests.Features.Trips.Commands;

public class CreateTripCommandHandlerTests
{
    private readonly ITripRepository _tripRepository = Substitute.For<ITripRepository>();
    private readonly IPricingGrpcService _pricingGrpcService = Substitute.For<IPricingGrpcService>();

    private CreateTripHandler CreateHandler() => new(_tripRepository, _pricingGrpcService);

    [Fact]
    public async Task Handle_WhenPricingGrpcSucceeds_CreatesTripWithPricingAndReturnsSuccess()
    {
        // Arrange
        var command = new CreateTripCommand(
            RiderId: Guid.NewGuid(),
            PickupAddress: "227 Nguyen Van Cu, D5",
            DropoffAddress: "Ben Thanh Market, D1",
            PickupLat: 10.762622,
            PickupLng: 106.682373,
            DropoffLat: 10.772545,
            DropoffLng: 106.698032,
            HasToll: true,
            PromoCode: "DISCOUNT10",
            IsRaining: true
        );

        var pricingDto = new PricingDetailsDto(
            DistanceFare: 40000.0,
            TimeFare: 15000.0,
            TaxRate: 0.08,
            BaseFare: 12000.0,
            RetentionFactor: 0.2,
            FareAfterSurge: 65000.0,
            FareAfterDiscount: 58000.0,
            TollFee: 10000.0,
            AppliedSurges: new List<AppliedSurgeDto>
            {
                new("WEATHER", "Mưa gió", 1.2)
            },
            DiscountAmount: 7000.0,
            TaxAmount: 4640.0,
            FinalTotal: 62640.0,
            DistanceKm: 6.8,
            EstimatedMinutes: 18
        );

        _pricingGrpcService.GetPricingAsync(
            Arg.Any<Guid>(),
            command.PickupLat,
            command.PickupLng,
            command.DropoffLat,
            command.DropoffLng,
            command.HasToll,
            command.PromoCode,
            command.IsRaining,
            Arg.Any<CancellationToken>())
            .Returns(pricingDto);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(command.RiderId, result.Value.RiderId);
        Assert.Equal(TripStatus.Requested.ToString(), result.Value.Status);
        Assert.Equal(62640m, result.Value.EstimatedFare);
        Assert.Equal(6.8m, result.Value.DistanceKm);
        Assert.NotNull(result.Value.Pricing);
        Assert.Equal(62640.0, result.Value.Pricing.FinalTotal);

        await _tripRepository.Received(1).CreateAsync(Arg.Is<TripEntity>(t =>
            t.RiderId == command.RiderId &&
            t.PickupAddress == command.PickupAddress &&
            t.Status == TripStatus.Requested));
        await _tripRepository.Received(1).Update(Arg.Any<TripEntity>());
        await _tripRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WhenPricingGrpcThrows_FallsBackToDefaultPricingAndReturnsSuccess()
    {
        // Arrange
        var command = new CreateTripCommand(
            RiderId: Guid.NewGuid(),
            PickupAddress: "123 Le Loi",
            DropoffAddress: "456 Nguyen Hue",
            PickupLat: 10.7769,
            PickupLng: 106.7009,
            DropoffLat: 10.7770,
            DropoffLng: 106.7010,
            HasToll: false
        );

        _pricingGrpcService.GetPricingAsync(
            Arg.Any<Guid>(),
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Matching Service unavailable"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(50000m, result.Value.EstimatedFare);
        Assert.Equal(5.0m, result.Value.DistanceKm);
        Assert.NotNull(result.Value.Pricing);
        Assert.Equal(50000.0, result.Value.Pricing.FinalTotal);

        await _tripRepository.Received(1).CreateAsync(Arg.Any<TripEntity>());
        await _tripRepository.Received(1).Update(Arg.Any<TripEntity>());
        await _tripRepository.Received(1).SaveChangesAsync();
    }
}
