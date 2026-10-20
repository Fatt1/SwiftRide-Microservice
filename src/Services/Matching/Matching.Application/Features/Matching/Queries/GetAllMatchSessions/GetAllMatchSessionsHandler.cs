using Matching.Application.Features.Matching.Dtos;
using Matching.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Matching.Application.Features.Matching.Queries.GetAllMatchSessions;

internal sealed class GetAllMatchSessionsHandler(IMatchingRepository matchingRepository)
    : IQueryHandler<GetAllMatchSessionsQuery, IReadOnlyList<MatchSessionDto>>
{
    public async Task<Result<IReadOnlyList<MatchSessionDto>>> Handle(
        GetAllMatchSessionsQuery request, 
        CancellationToken cancellationToken)
    {
        var sessions = await matchingRepository.GetAllMatchingSessionAsync(cancellationToken);

        var dtos = sessions.Select(s => new MatchSessionDto(
            s.Id,
            s.TripId,
            s.PickupLat,
            s.PickupLng,
            s.DistanceKm,
            s.EstimatedMinutes,
            s.PricingBreakdown,
            s.MatchedDriverId,
            s.DriverAttempts,
            s.Status.ToString(),
            s.CreatedAt,
            s.LastModifiedAt
        )).ToList();

        return Result.Success<IReadOnlyList<MatchSessionDto>>(dtos);
    }
}
