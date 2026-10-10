using Matching.Application.Features.Matching.Commands.FindDriver;
using MediatR;
using Shared.Extensions;

namespace Matching.API.Endpoints;

public static class MatchingEndpoints
{
    public static IEndpointRouteBuilder MapMatchingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapApiV1Group("matching");

        group.MapPost("/match", async (FindDriverCommand command, ISender sender) =>
        {
            var result = await sender.Send(command, CancellationToken.None);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        });
        return endpoints;
    }

}
