using Grpc.Core;
using Matching.API.Protos;
using Matching.API.Services;
using Matching.Application.Features.Matching.Commands.CalculatePrice;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared;
using Xunit;

namespace Matching.UnitTests.Grpc;

public class PricingGrpcServiceTests
{
    private readonly ILogger<PricingGrpcService> _logger = Substitute.For<ILogger<PricingGrpcService>>();
    private readonly ISender _mediator = Substitute.For<ISender>();

    private PricingGrpcService CreateService() => new(_logger, _mediator);

    [Fact]
    public async Task GetPricing_WithInvalidTripId_ThrowsRpcExceptionWithInvalidArgument()
    {
        // Arrange
        var service = CreateService();
        var request = new GetPricingRequest
        {
            TripId = "invalid-not-a-guid",
            PickupLatitude = 10.7725,
            PickupLongitude = 106.6980,
            DestinationLatitude = 10.7950,
            DestinationLongitude = 106.7218
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetPricing(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.Contains("must be a valid GUID", ex.Status.Detail);
    }

    [Fact]
    public async Task GetPricing_WithValidRequest_ReturnsPricingResponse()
    {
        // Arrange
        var service = CreateService();
        var tripId = Guid.NewGuid();
        var request = new GetPricingRequest
        {
            TripId = tripId.ToString(),
            PickupLatitude = 10.7725,
            PickupLongitude = 106.6980,
            DestinationLatitude = 10.7950,
            DestinationLongitude = 106.7218,
            IsRaining = false,
            HasToll = false
        };

        var calculatePriceDto = new CalculatePriceDto
        {
            DistanceKm = 4.0,
            EstimatedMinutes = 12,
            BaseFare = 32000,
            DistanceFare = 32000,
            TimeFare = 6000,
            FinalTotal = 42000,
            TaxRate = 0.1,
            TaxAmount = 4000
        };

        _mediator.Send(Arg.Any<CalculatePriceCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CalculatePriceDto>.Success(calculatePriceDto));

        // Act
        var response = await service.GetPricing(request, Substitute.For<ServerCallContext>());

        // Assert
        Assert.NotNull(response);
        Assert.Equal(42000, response.FinalTotal);
        Assert.Equal(4.0, response.DistanceKm);
        Assert.Equal(12.0, response.EstimatedMinutes);
    }
}
