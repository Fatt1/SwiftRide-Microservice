using MediatR;
using Payment.Application.Features.Wallets.Commands.TopUpWallet;
using Payment.Application.Features.Wallets.Queries.GetWallets;
using Shared.Extensions;

namespace Payment.API.Endpoints;

public static class WalletEndpoints
{
    public sealed record TopUpRequest(Guid RequestId, decimal Amount);

    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapApiV1Group("wallets").WithTags("Wallets");

        group.MapGet("", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetWalletsQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        group.MapPost("/{userId:guid}/top-up", async (Guid userId, TopUpRequest request,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new TopUpWalletCommand(userId, request.RequestId, request.Amount), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        return endpoints;
    }
}
