using Matching.Application.Features.Matching.Dtos;
using Matching.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Matching.Application.Features.Matching.Queries.GetAllDriverLocations;

internal sealed class GetAllDriverLocationsHandler(IDriverLocationRepository driverLocationRepository)
    : IQueryHandler<GetAllDriverLocationsQuery, IReadOnlyList<DriverLocationDto>>
{
    public async Task<Result<IReadOnlyList<DriverLocationDto>>> Handle(
        GetAllDriverLocationsQuery request, 
        CancellationToken cancellationToken)
    {
        var locations = await driverLocationRepository.GetAllAsync(cancellationToken);

        var dtos = locations.Select(l => new DriverLocationDto(
            l.Id,
            l.DriverId,
            l.FullName,
            l.Latitude,
            l.Longitude,
            l.IsAvailable,
            l.UpdatedAt
        )).ToList();

        return Result.Success<IReadOnlyList<DriverLocationDto>>(dtos);
    }
}
