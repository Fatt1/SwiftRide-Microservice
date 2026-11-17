namespace Matching.Application.Abstractions;
public sealed record RouteEstimate(double DistanceKm, int EstimatedMinutes);
public interface IRouteEstimator
{
    Task<RouteEstimate> EstimateAsync(double originLat, double originLng, double destinationLat, double destinationLng, CancellationToken ct);
}
