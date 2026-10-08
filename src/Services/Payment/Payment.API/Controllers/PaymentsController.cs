using MediatR;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Payments;
using Shared;
using Shared.Enums.Payments;

namespace Payment.API.Controllers;

[ApiController]
[Route("payments")]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreatePaymentRequest body,
        [FromHeader(Name = "Idempotency-Key")] Guid key, CancellationToken ct)
    {
        var result = await sender.Send(new CreatePaymentCommand(body.TripId, body.RiderId, body.DriverId,
            body.Amount, body.PaymentMethod, key, body.CorrelationId, body.GatewayToken, body.Currency), ct);
        if (result.IsFailure) return Error(result.Error!);
        var p = result.Value;
        var status = p.Status == "Pending" ? 202 : p.Status == "Failed" ? 422 : p.Created ? 201 : 200;
        return StatusCode(status, p);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetPaymentQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : Error(result.Error!);
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, RefundRequest body,
        [FromHeader(Name = "Idempotency-Key")] Guid key, CancellationToken ct)
    {
        var result = await sender.Send(new RefundPaymentCommand(id, key, body.Reason), ct);
        if (result.IsFailure) return Error(result.Error!);
        var r = result.Value;
        return StatusCode(r.Status == "Pending" ? 202 : r.Status == "Failed" ? 422 : r.Created ? 201 : 200, r);
    }

    private ObjectResult Error(Error error)
    {
        var status = error switch { NotFoundError => 404, ForbiddenError => 403, ConflictError => 409, _ => 400 };
        var details = new ProblemDetails { Title = error.Code, Detail = error.Message, Status = status };
        if (error is ValidationError validation) details.Extensions["errors"] = validation.Errors;
        return StatusCode(status, details);
    }
}

public sealed record CreatePaymentRequest(Guid TripId, Guid RiderId, Guid DriverId, decimal Amount,
    PaymentMethod PaymentMethod, Guid CorrelationId, string? GatewayToken = null, string Currency = "VND");
public sealed record RefundRequest(string Reason);
