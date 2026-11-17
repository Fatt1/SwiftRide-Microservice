using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared;
using Trip.Application.DTOs;
using Trip.Application.Features.Trips.Commands.CreateTrip;
using Trip.Application.Interfaces;
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
            EstimatedMinutes: 18, MatchSessionId: "507f1f77bcf86cd799439011"
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
            Arg.Any<double?>(),
            Arg.Any<int?>(),
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
    }

    [Fact]
    public async Task Handle_WhenRoutingUnavailable_ReturnsFailureWithoutSavingTrip()
    {
        // Arrange
        var command = new CreateTripCommand(Guid.NewGuid(), "Pickup", "Dropoff", 10.7, 106.7, 10.8, 106.8);
        _pricingGrpcService.GetPricingAsync(Arg.Any<Guid>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<double>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<double?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Throws(new PricingUnavailableException("OSRM unavailable"));
        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);
        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<UnavailableError>(result.Error);
        await _tripRepository.DidNotReceive().CreateAsync(Arg.Any<TripEntity>());
    }
}
