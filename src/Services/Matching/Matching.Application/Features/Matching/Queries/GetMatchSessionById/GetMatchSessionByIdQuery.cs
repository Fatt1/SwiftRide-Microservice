using Matching.Application.Features.Matching.Dtos;
using Shared.CQRS;

namespace Matching.Application.Features.Matching.Queries.GetMatchSessionById;

public sealed record GetMatchSessionByIdQuery(string Id) : IQuery<MatchSessionDto>;
