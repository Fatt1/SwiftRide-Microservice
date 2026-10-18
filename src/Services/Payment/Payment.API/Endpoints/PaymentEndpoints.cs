using MediatR;
using Payment.Application.Features.Payments.Commands.CreatePayment;
using Payment.Application.Features.Payments.Commands.RefundPayment;
using Payment.Application.Features.Payments.Queries.GetPaymentById;
using Shared.Extensions;

namespace Payment.API.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapApiV1Group("payments").WithTags("Payments");
        group.MapPost("", async (CreatePaymentCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Thanh toán một chuyến đi")
        .WithDescription("Gửi TripId, RiderId, DriverId, số tiền, phương thức thanh toán và IdempotencyKey. Với thẻ, cần thêm GatewayToken. API trả về PaymentId và trạng thái; gửi lại cùng IdempotencyKey sẽ trả giao dịch đã có.");
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPaymentByIdQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Xem chi tiết giao dịch theo PaymentId")
        .WithDescription("Nhập PaymentId vào tham số id. Trả về số tiền, loại giao dịch, phương thức và trạng thái; trả 404 nếu không tìm thấy.");
        group.MapPost("/{id:guid}/refund", async (Guid id, RefundPaymentCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command with { PaymentId = id }, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Hoàn tiền cho giao dịch thanh toán chuyến")
        .WithDescription("Nhập PaymentId vào id và lý do hoàn tiền vào body. Chỉ hoàn tiền cho TripPayment đã Completed; gọi lại sẽ trả khoản hoàn tiền đã tạo.");
        return endpoints;
    }
}
