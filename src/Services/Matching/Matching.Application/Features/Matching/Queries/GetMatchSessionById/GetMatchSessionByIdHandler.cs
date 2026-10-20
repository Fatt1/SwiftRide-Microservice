using Matching.Application.Features.Matching.Dtos;
using Matching.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Matching.Application.Features.Matching.Queries.GetMatchSessionById;

internal sealed class GetMatchSessionByIdHandler(IMatchingRepository matchingRepository)
    : IQueryHandler<GetMatchSessionByIdQuery, MatchSessionDto>
{
    public async Task<Result<MatchSessionDto>> Handle(
        GetMatchSessionByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var s = await matchingRepository.GetByIdAsync(request.Id, cancellationToken);
        if (s is null)
        {
            return Result.Failure<MatchSessionDto>(new NotFoundError("MatchSession", request.Id));
        }

        var dto = new MatchSessionDto(
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
        );

        return Result.Success(dto);
    }
}
