using System.Net;
using Matching.Infrastructure.Routing;
using Microsoft.Extensions.Options;
using Xunit;
namespace Matching.UnitTests.Routing;
public class OsrmRouteEstimatorTests
{
    private sealed class StubHandler(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public Uri? RequestedUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestedUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json) });
        }
    }
    [Fact]
    public async Task Estimate_RoadRoute_UsesLngLatAndConvertsMetersAndSeconds()
    {
        // Arrange
        var handler = new StubHandler("{\"code\":\"Ok\",\"routes\":[{\"distance\":4250,\"duration\":601}]}");
        using var client = new HttpClient(handler);
        var estimator = new OsrmRouteEstimator(client, Options.Create(new OsrmOptions()));
        // Act
        var estimate = await estimator.EstimateAsync(10.7, 106.7, 10.8, 106.8, default);
        // Assert
        Assert.Equal(4.25, estimate.DistanceKm); Assert.Equal(11, estimate.EstimatedMinutes);
        Assert.Contains("/route/v1/driving/106.7,10.7;106.8,10.8", handler.RequestedUri!.OriginalString);
    }
    [Theory]
    [InlineData("{\"code\":\"NoRoute\",\"routes\":[]}", 200)]
    [InlineData("unavailable", 503)]
    public async Task Estimate_RoutingFailure_ThrowsWithoutFallback(string response, int status)
    {
        // Arrange
        using var client = new HttpClient(new StubHandler(response, (HttpStatusCode)status));
        var estimator = new OsrmRouteEstimator(client, Options.Create(new OsrmOptions()));
        // Act / Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => estimator.EstimateAsync(10.7, 106.7, 10.8, 106.8, default));
    }
    [Fact]
    public async Task Estimate_CustomMotorcycleEndpoint_UsesConfiguredProfile()
    {
        // Arrange
        var handler = new StubHandler("{\"code\":\"Ok\",\"routes\":[{\"distance\":1000,\"duration\":60}]}");
        using var client = new HttpClient(handler);
        var estimator = new OsrmRouteEstimator(client, Options.Create(new OsrmOptions { BaseUrl = "https://routing.example", Profile = "motorcycle" }));
        // Act
        var estimate = await estimator.EstimateAsync(10, 106, 11, 107, default);
        // Assert
        Assert.Contains("/route/v1/motorcycle/", handler.RequestedUri!.OriginalString); Assert.Equal(1, estimate.DistanceKm);
    }
}
