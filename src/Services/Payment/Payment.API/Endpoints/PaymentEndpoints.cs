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
        });
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPaymentByIdQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });
        group.MapPost("/{id:guid}/refund", async (Guid id, RefundPaymentCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command with { PaymentId = id }, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });
        return endpoints;
    }
}
