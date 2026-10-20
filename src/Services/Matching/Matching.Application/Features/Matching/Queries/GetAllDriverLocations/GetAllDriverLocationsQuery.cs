using Matching.Application.Features.Matching.Dtos;
using Shared.CQRS;

namespace Matching.Application.Features.Matching.Queries.GetAllDriverLocations;

public sealed record GetAllDriverLocationsQuery : IQuery<IReadOnlyList<DriverLocationDto>>;
