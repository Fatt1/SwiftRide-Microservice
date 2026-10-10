namespace Matching.Application.Helpers;

/// <summary>
/// Helper hỗ trợ tính toán khoảng cách địa lý và thời gian di chuyển ước tính.
/// </summary>
public static class GeoHelper
{
    /// <summary>
    /// Bán kính Trái Đất trung bình (đơn vị: km).
    /// </summary>
    public const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Tốc độ trung bình mặc định trong khu vực đô thị (đơn vị: km/h).
    /// </summary>
    public const double DefaultAverageSpeedKmH = 30.0;

    /// <summary>
    /// Tính khoảng cách giữa hai tọa độ (kinh độ, vĩ độ) theo công thức Haversine (đơn vị: kilômét).
    /// </summary>
    /// <param name="originLat">Vĩ độ điểm đón / điểm đi (Origin Latitude).</param>
    /// <param name="originLon">Kinh độ điểm đón / điểm đi (Origin Longitude).</param>
    /// <param name="destLat">Vĩ độ điểm đến (Destination Latitude).</param>
    /// <param name="destLon">Kinh độ điểm đến (Destination Longitude).</param>
    /// <returns>Khoảng cách tính theo km.</returns>
    public static double CalculateDistanceInKm(double originLat, double originLon, double destLat, double destLon)
    {
        if (Math.Abs(originLat - destLat) < double.Epsilon && Math.Abs(originLon - destLon) < double.Epsilon)
        {
            return 0.0;
        }

        var dLat = ToRadians(destLat - originLat);
        var dLon = ToRadians(destLon - originLon);

        var rLat1 = ToRadians(originLat);
        var rLat2 = ToRadians(destLat);

        var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                Math.Cos(rLat1) * Math.Cos(rLat2) *
                Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        return EarthRadiusKm * c;
    }

    /// <summary>
    /// Tính khoảng cách giữa hai tọa độ theo đơn vị mét (m).
    /// </summary>
    public static double CalculateDistanceInMeters(double originLat, double originLon, double destLat, double destLon)
    {
        return CalculateDistanceInKm(originLat, originLon, destLat, destLon) * 1000.0;
    }

    /// <summary>
    /// Tính thời gian di chuyển ước tính dưới dạng TimeSpan dựa trên khoảng cách (km) và tốc độ trung bình (km/h).
    /// </summary>
    /// <param name="distanceKm">Khoảng cách di chuyển (km).</param>
    /// <param name="averageSpeedKmPerHour">Tốc độ trung bình (km/h), mặc định 30 km/h.</param>
    /// <returns>Thời gian di chuyển ước tính dưới dạng TimeSpan.</returns>
    public static TimeSpan CalculateEstimatedTime(double distanceKm, double averageSpeedKmPerHour = DefaultAverageSpeedKmH)
    {
        if (averageSpeedKmPerHour <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(averageSpeedKmPerHour), "Tốc độ trung bình phải lớn hơn 0.");
        }

        if (distanceKm <= 0)
        {
            return TimeSpan.Zero;
        }

        var hours = distanceKm / averageSpeedKmPerHour;
        return TimeSpan.FromHours(hours);
    }

    /// <summary>
    /// Tính thời gian di chuyển ước tính theo số phút (làm tròn lên phút gần nhất, tối thiểu 1 phút nếu có di chuyển).
    /// </summary>
    /// <param name="distanceKm">Khoảng cách di chuyển (km).</param>
    /// <param name="averageSpeedKmPerHour">Tốc độ trung bình (km/h), mặc định 30 km/h.</param>
    /// <returns>Số phút ước tính (int).</returns>
    public static int CalculateEstimatedMinutes(double distanceKm, double averageSpeedKmPerHour = DefaultAverageSpeedKmH)
    {
        if (distanceKm <= 0)
        {
            return 0;
        }

        var duration = CalculateEstimatedTime(distanceKm, averageSpeedKmPerHour);
        return Math.Max(1, (int)Math.Round(duration.TotalMinutes, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Tính toán cả khoảng cách và thời gian di chuyển ước tính giữa hai tọa độ.
    /// Khoảng cách được làm tròn đến 1 chữ số thập phân, thời gian làm tròn đến phút gần nhất.
    /// </summary>
    /// <param name="originLat">Vĩ độ điểm đón / điểm đi.</param>
    /// <param name="originLon">Kinh độ điểm đón / điểm đi.</param>
    /// <param name="destLat">Vĩ độ điểm đến.</param>
    /// <param name="destLon">Kinh độ điểm đến.</param>
    /// <param name="averageSpeedKmPerHour">Tốc độ trung bình (km/h), mặc định 30 km/h.</param>
    /// <returns>Tuple chứa (DistanceKm, EstimatedMinutes, Duration).</returns>
    public static (double DistanceKm, int EstimatedMinutes, TimeSpan Duration) CalculateTravelEstimate(
        double originLat,
        double originLon,
        double destLat,
        double destLon,
        double averageSpeedKmPerHour = DefaultAverageSpeedKmH)
    {
        var rawDistance = CalculateDistanceInKm(originLat, originLon, destLat, destLon);
        var distanceKm = Math.Round(rawDistance, 1, MidpointRounding.AwayFromZero);
        var duration = CalculateEstimatedTime(distanceKm, averageSpeedKmPerHour);
        var estimatedMinutes = distanceKm <= 0 ? 0 : Math.Max(1, (int)Math.Round(duration.TotalMinutes, MidpointRounding.AwayFromZero));

        return (distanceKm, estimatedMinutes, duration);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}

