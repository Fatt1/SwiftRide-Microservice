using Matching.Application.Features.Matching.Commands.FindDriver;
using Matching.Application.Features.Matching.Queries.GetAllDriverLocations;
using Matching.Application.Features.Matching.Queries.GetAllMatchSessions;
using Matching.Application.Features.Matching.Queries.GetMatchSessionById;
using MediatR;
using Shared.Extensions;

namespace Matching.API.Endpoints;

public static class MatchingEndpoints
{
    public static IEndpointRouteBuilder MapMatchingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapApiV1Group("matching");

        // 1. Tìm tài xế phù hợp cho chuyến đi (Command)
        group.MapPost("/match", async (FindDriverCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        });

        // 2. Lấy danh sách vị trí tất cả tài xế (Query)
        group.MapGet("/drivers/locations", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAllDriverLocationsQuery(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        });

        // 3. Lấy danh sách tất cả các phiên matching (Query)
        group.MapGet("/matchings", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAllMatchSessionsQuery(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        });

        // Alias hỗ trợ endpoint /sessions
        group.MapGet("/sessions", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAllMatchSessionsQuery(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        });

        // 4. Lấy chi tiết phiên matching theo ID (Query)
        group.MapGet("/matchings/{id}", async (string id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMatchSessionByIdQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        });

        return endpoints;
    }
}
