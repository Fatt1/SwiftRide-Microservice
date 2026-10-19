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
        var group = endpoints.MapApiV1Group("wallets").WithTags("Wallets");

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
        })
        .WithSummary("Nạp tiền vào ví của người dùng")
        .WithDescription("Nhập UserId vào đường dẫn; body gồm IdempotencyKey duy nhất và Amount lớn hơn 0. API cộng tiền, tạo giao dịch WalletTopUp và dòng ledger Credit, rồi trả về ví với số dư mới. Gửi lại cùng IdempotencyKey không cộng tiền lần nữa.");

        return endpoints;
    }
}
