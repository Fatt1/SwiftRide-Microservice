using Grpc.Core;
using Matching.API.Protos;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Trip.Infrastructure.GrpcServices;
using Xunit;

namespace Trip.UnitTests.Grpc;

public class PricingGrpcServiceTests
{
    private readonly PricingService.PricingServiceClient _client = Substitute.For<PricingService.PricingServiceClient>();
    private readonly ILogger<PricingGrpcService> _logger = Substitute.For<ILogger<PricingGrpcService>>();

    private static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    [Fact]
    public void Constructor_WhenClientIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PricingGrpcService(null!, _logger));
    }

    [Fact]
    public async Task GetPricingAsync_WhenCallSucceeds_ReturnsMappedPricingDetailsDto()
    {
        // Arrange
        var service = new PricingGrpcService(_client, _logger);
        var tripId = Guid.NewGuid();

        var grpcResponse = new PricingResponse
        {
            DistanceFare = 40000.0,
            TimeFare = 10000.0,
            TaxRate = 0.1,
            BaseFare = 15000.0,
            RetentionFactor = 0.2,
            FareAfterSurge = 55000.0,
            FareAfterDiscount = 50000.0,
            TollFee = 10000.0,
            DiscountAmount = 5000.0,
            TaxAmount = 5000.0,
            FinalTotal = 55000.0,
            DistanceKm = 5.5,
            EstimatedMinutes = 15
        };
        grpcResponse.AppliedSurges.Add(new AppliedSurge
        {
            Type = "PEAK_HOUR",
            Name = "Giờ cao điểm",
            Multiplier = 1.3
        });

        using var call = CreateAsyncUnaryCall(grpcResponse);
        _client.GetPricingAsync(
                Arg.Any<GetPricingRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(call);

        // Act
        var result = await service.GetPricingAsync(
            tripId, 10.762622, 106.682373, 10.772545, 106.698032, hasToll: true, promoCode: "PROMO", isRaining: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(55000.0, result.FinalTotal);
        Assert.Equal(5.5, result.DistanceKm);
        Assert.Equal(15, result.EstimatedMinutes);
        Assert.Single(result.AppliedSurges);
        Assert.Equal("PEAK_HOUR", result.AppliedSurges[0].Type);
    }

    [Fact]
    public async Task GetPricingAsync_WhenGrpcThrows_LogsErrorAndRethrows()
    {
        // Arrange
        var service = new PricingGrpcService(_client, _logger);
        var tripId = Guid.NewGuid();

        _client.GetPricingAsync(
                Arg.Any<GetPricingRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "Service is down")));

        // Act & Assert
        await Assert.ThrowsAsync<RpcException>(() =>
            service.GetPricingAsync(tripId, 10.0, 106.0, 10.1, 106.1));
    }
}
