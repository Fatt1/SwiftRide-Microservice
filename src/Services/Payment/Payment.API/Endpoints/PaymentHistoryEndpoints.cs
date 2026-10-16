using MediatR;
using Payment.Application.Features.Ledger.Queries.GetLedger;
using Payment.Application.Features.Payments.Queries.GetPayments;
using Shared.Extensions;

namespace Payment.API.Endpoints;

public static class PaymentHistoryEndpoints
{
    public static IEndpointRouteBuilder MapPaymentHistoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapApiV1Group("payments").WithTags("Payments")
            .MapGet("", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetPaymentsQuery(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
            });

        endpoints.MapApiV1Group("ledger").WithTags("Ledger")
            .MapGet("", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetLedgerQuery(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
            });

        return endpoints;
    }
}
