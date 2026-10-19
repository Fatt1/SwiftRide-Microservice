using MediatR;
using Payment.Application.Features.Ledger.Queries.GetLedger;
using Payment.Application.Features.Ledger.Queries.GetLedgerByPaymentId;
using Payment.Application.Features.Payments.Queries.GetPayments;
using Payment.Application.Features.Payments.Queries.GetPaymentByTripId;
using Shared.Extensions;

namespace Payment.API.Endpoints;

public static class PaymentHistoryEndpoints
{
    public static IEndpointRouteBuilder MapPaymentHistoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var paymentsGroup = endpoints.MapApiV1Group("payments").WithTags("Payments");
        paymentsGroup
            .MapGet("", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetPaymentsQuery(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
            })
            .WithSummary("Xem lịch sử giao dịch thanh toán")
            .WithDescription("Trả về các PaymentTransaction, gồm thanh toán chuyến và nạp ví. Lấy PaymentId từ danh sách để gọi API xem chi tiết.");

        paymentsGroup.MapGet("/trip/{tripId:guid}", async (Guid tripId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetPaymentByTripIdQuery(tripId), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
            })
            .WithSummary("Xem giao dịch thanh toán theo TripId")
            .WithDescription("Trả về giao dịch của chuyến đi, gồm trạng thái và lý do thất bại; trả 404 nếu chuyến chưa có giao dịch.");

        var ledgerGroup = endpoints.MapApiV1Group("ledger").WithTags("Ledger");
        ledgerGroup
            .MapGet("", async (ISender sender, CancellationToken ct) =>
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
