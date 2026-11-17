using System.Text.Json;
using Matching.Application.Abstractions;
using Microsoft.Extensions.Options;
namespace Matching.Infrastructure.Routing;

public sealed class OsrmOptions
{
    public string BaseUrl { get; set; } = "https://router.project-osrm.org";
    // Use a custom motorcycle endpoint/profile when available; stock OSRM uses driving.
    public string Profile { get; set; } = "driving";
}
public sealed class OsrmRouteEstimator(HttpClient client, IOptions<OsrmOptions> options) : IRouteEstimator
{
    public async Task<RouteEstimate> EstimateAsync(double originLat, double originLng, double destinationLat, double destinationLng, CancellationToken ct)
    {
        if (!Valid(originLat, originLng) || !Valid(destinationLat, destinationLng))
            throw new ArgumentException("Invalid route coordinates.");
        var o = options.Value;

        var coordinates = FormattableString.Invariant($"{originLng},{originLat};{destinationLng},{destinationLat}");
        var url = $"{o.BaseUrl.TrimEnd('/')}/route/v1/{Uri.EscapeDataString(o.Profile)}/{coordinates}?overview=false&steps=false";
        using var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var body = json.RootElement;
        if (body.GetProperty("code").GetString() != "Ok" || body.GetProperty("routes").GetArrayLength() == 0)
            throw new HttpRequestException("OSRM could not find a road route.");

        var route = body.GetProperty("routes")[0];
        var meters = route.GetProperty("distance").GetDouble();
        var seconds = route.GetProperty("duration").GetDouble();

        if (!double.IsFinite(meters) || !double.IsFinite(seconds) || meters < 0 || seconds < 0)
            throw new HttpRequestException("OSRM returned an invalid estimate.");
        return new RouteEstimate(meters / 1000d, Math.Max(1, (int)Math.Ceiling(seconds / 60d)));
    }
    private static bool Valid(double lat, double lng)
        => double.IsFinite(lat) && double.IsFinite(lng) && Math.Abs(lat) <= 90 && Math.Abs(lng) <= 180;
}
