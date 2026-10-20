using MediatR;
using Payment.Application.Features.Wallets.Commands.TopUpWallet;
using Payment.Application.Features.Wallets.Queries.GetWallets;
using Shared.Extensions;

namespace Payment.API.Endpoints;

public static class WalletEndpoints
{
    public sealed record TopUpRequest(Guid IdempotencyKey, decimal Amount);

    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapApiV1Group("payments/wallets").WithTags("Wallets");

        group.MapGet("", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetWalletsQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Xem danh sách ví")
        .WithDescription("Trả về các ví cùng UserId, loại tiền và số dư hiện tại.");

        group.MapPost("/{userId:guid}/top-up", async (Guid userId, TopUpRequest request,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new TopUpWalletCommand(userId, request.IdempotencyKey, request.Amount), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        group.MapGet("/user/{userId:guid}", async (Guid userId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new Payment.Application.Features.Wallets.Queries.GetWalletByUserId.GetWalletByUserIdQuery(userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Lấy thông tin ví theo UserId")
        .WithDescription("Trả về thông tin ví của người dùng (tự động tạo ví mới nếu chưa có).");

        group.MapGet("/user/{userId:guid}/ledger", async (Guid userId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new Payment.Application.Features.Wallets.Queries.GetLedgerByUserId.GetLedgerByUserIdQuery(userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Lấy lịch sử giao dịch sổ cái (ledger) theo UserId")
        .WithDescription("Trả về danh sách các biến động số dư (nạp tiền, trừ tiền cuốc xe, v.v.) của ví.");

        return endpoints;
    }
}
