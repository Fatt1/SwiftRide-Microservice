using MediatR;
using Payment.Application.Features.Ledger.Queries.GetLedger;
using Payment.Application.Features.Ledger.Queries.GetLedgerByPaymentId;
using Payment.Application.Features.Payments.Commands.CreatePayment;
using Payment.Application.Features.Payments.Commands.RefundPayment;
using Payment.Application.Features.Payments.Queries.GetPaymentById;
using Payment.Application.Features.Payments.Queries.GetPayments;
using Payment.Application.Features.Payments.Queries.GetPaymentByTripId;
using Shared.Extensions;

namespace Payment.API.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapApiV1Group("payments").WithTags("Payments");

        // 1. Tạo giao dịch thanh toán chuyến đi
        group.MapPost("", async (CreatePaymentCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Thanh toán một chuyến đi")
        .WithDescription("Gửi TripId, RiderId, DriverId, số tiền, phương thức thanh toán và IdempotencyKey. Với thẻ, cần thêm GatewayToken. API trả về PaymentId và trạng thái; gửi lại cùng IdempotencyKey sẽ trả giao dịch đã có.");

        // 2. Lấy danh sách tất cả giao dịch thanh toán
        group.MapGet("", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPaymentsQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Xem lịch sử giao dịch thanh toán")
        .WithDescription("Trả về các PaymentTransaction, gồm thanh toán chuyến và nạp ví. Lấy PaymentId từ danh sách để gọi API xem chi tiết.");

        // 3. Xem chi tiết giao dịch theo PaymentId
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPaymentByIdQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Xem chi tiết giao dịch theo PaymentId")
        .WithDescription("Nhập PaymentId vào tham số id. Trả về số tiền, loại giao dịch, phương thức và trạng thái; trả 404 nếu không tìm thấy.");

        // 4. Xem giao dịch thanh toán theo TripId
        group.MapGet("/trip/{tripId:guid}", async (Guid tripId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPaymentByTripIdQuery(tripId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Xem giao dịch thanh toán theo TripId")
        .WithDescription("Trả về giao dịch của chuyến đi, gồm trạng thái và lý do thất bại; trả 404 nếu chuyến chưa có giao dịch.");

        // 5. Hoàn tiền cho giao dịch thanh toán chuyến
        group.MapPost("/{id:guid}/refund", async (Guid id, RefundPaymentCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command with { PaymentId = id }, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Hoàn tiền cho giao dịch thanh toán chuyến")
        .WithDescription("Nhập PaymentId vào id và lý do hoàn tiền vào body. Chỉ hoàn tiền cho TripPayment đã Completed; gọi lại sẽ trả khoản hoàn tiền đã tạo.");

        // 6. Quản lý sổ cái biến động tiền (Ledger)
        var ledgerGroup = endpoints.MapApiV1Group("payments/ledger").WithTags("Ledger");

        ledgerGroup.MapGet("", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetLedgerQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Xem lịch sử biến động tiền")
        .WithDescription("Trả về các dòng LedgerEntry. Debit là tiền giảm, Credit là tiền tăng; WalletId chỉ ví bị ảnh hưởng và PaymentId liên kết với giao dịch.");

        ledgerGroup.MapGet("/payment/{paymentId:guid}", async (Guid paymentId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetLedgerByPaymentIdQuery(paymentId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithSummary("Xem biến động ví theo PaymentId")
        .WithDescription("Trả về các dòng Debit/Credit của giao dịch; danh sách rỗng nếu giao dịch không làm thay đổi ví, 404 nếu PaymentId không tồn tại.");

        return endpoints;
    }
}
