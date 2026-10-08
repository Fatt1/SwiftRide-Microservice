using Matching.Domain.Entities;
using Matching.Domain.Repositories;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Matching.Infrastructure.Repositories;

public class DriverLocationRepository : IDriverLocationRepository
{
    private readonly IMongoCollection<DriverLocation> _driverLocations;

    public DriverLocationRepository(IMongoDatabase database)
    {
        _driverLocations = database.GetCollection<DriverLocation>("driver_locations");
    }
    public Task CreateAsync(DriverLocation driverLocation, CancellationToken ct = default)
    {
        return _driverLocations.InsertOneAsync(driverLocation, cancellationToken: ct);
    }

    public async Task<DriverLocation?> GetByDriverIdAsync(Guid driverId, CancellationToken ct = default)
    {
        var filter = Builders<DriverLocation>.Filter.Eq(x => x.DriverId, driverId);
        return await _driverLocations.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<DriverLocation?> GetNearestAvailableDriverAsync(double latitude, double longitude, double maxRadiusInMeters, List<Guid> excludedDriverIds, CancellationToken ct = default)
    {
        var pickupPoint = GeoJson.Point(GeoJson.Geographic(longitude, latitude));

        var builder = Builders<DriverLocation>.Filter;

        var statusFilter = builder.Eq(x => x.IsAvailable, true);

        var exclusionFilter = excludedDriverIds.Count > 0
            ? builder.Nin(x => x.DriverId, excludedDriverIds)
            : builder.Empty;


        var geoFilter = builder.NearSphere(x => x.Location, pickupPoint, maxRadiusInMeters);

        var finalFilter = builder.And(statusFilter, exclusionFilter, geoFilter);

        return await _driverLocations.Find(finalFilter)
            .FirstOrDefaultAsync(ct);
    }

    public Task UpdateAsync(DriverLocation driverLocation, CancellationToken ct = default)
    {
        var filter = Builders<DriverLocation>.Filter.Eq(x => x.DriverId, driverLocation.DriverId);
        return _driverLocations.ReplaceOneAsync(filter, driverLocation, cancellationToken: ct);
    }
}
