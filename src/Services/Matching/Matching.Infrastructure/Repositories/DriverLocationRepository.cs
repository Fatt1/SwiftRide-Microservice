using Matching.Domain.Entities;
using Matching.Domain.Repositories;

namespace Matching.Infrastructure.Repositories;

public class DriverLocationRepository : IDriverLocationRepository
{
    public Task CreateAsync(DriverLocation driverLocation, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<DriverLocation?> GetByDriverIdAsync(Guid driverId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<DriverLocation?> GetNearestAvailableDriverAsync(double latitude, double longitude, double radiusInKm, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(DriverLocation driverLocation, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
