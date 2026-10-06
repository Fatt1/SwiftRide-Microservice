using Matching.Domain.Entities;

namespace Matching.Domain.Repositories;

public interface IDriverLocationRepository
{
    Task<DriverLocation?> GetByDriverIdAsync(Guid driverId, CancellationToken ct = default);

    Task CreateAsync(DriverLocation driverLocation, CancellationToken ct = default);

    Task UpdateAsync(DriverLocation driverLocation, CancellationToken ct = default);

    Task<DriverLocation?> GetNearestAvailableDriverAsync(double latitude, double longitude, double radiusInKm, CancellationToken ct = default);
}
