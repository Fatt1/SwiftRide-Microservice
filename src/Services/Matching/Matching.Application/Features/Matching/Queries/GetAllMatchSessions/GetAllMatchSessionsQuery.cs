using Matching.Application.Features.Matching.Dtos;
using Shared.CQRS;

namespace Matching.Application.Features.Matching.Queries.GetAllMatchSessions;

public sealed record GetAllMatchSessionsQuery : IQuery<IReadOnlyList<MatchSessionDto>>;
