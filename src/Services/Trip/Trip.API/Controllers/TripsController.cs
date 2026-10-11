using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.Payments;
using Shared.Extensions;
using Trip.Application.Features.Trips.Commands.CancelTrip;
using Trip.Application.Features.Trips.Commands.CreateTrip;
using Trip.Application.Features.Trips.Commands.DriverAccept;
using Trip.Application.Features.Trips.Commands.DriverReject;
using Trip.Application.Features.Trips.Commands.DropOff;
using Trip.Application.Features.Trips.Commands.PickUp;
using Trip.Application.Features.Trips.Queries.GetTripById;

namespace Trip.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController(ISender sender) : ControllerBase
{
    // 1. Đặt chuyến xe mới
    [HttpPost]
    public async Task<IResult> Create([FromBody] CreateTripCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.IsSuccess 
            ? Results.Ok(new { TripId = result.Value }) 
            : result.ToProblemDetails();
    }

    // 2. Lấy thông tin chuyến đi theo ID
    [HttpGet("{id:guid}")]
    public async Task<IResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetTripByIdQuery(id), ct);
        return result.IsSuccess 
            ? Results.Ok(result.Value) 
            : result.ToProblemDetails();
    }

    // 3. Tài xế nhận chuyến
    [HttpPost("{id:guid}/driver-accept")]
    public async Task<IResult> DriverAccept(Guid id, [FromQuery] Guid driverId, CancellationToken ct)
    {
        var result = await sender.Send(new DriverAcceptCommand(id, driverId), ct);
        return result.IsSuccess 
            ? Results.Ok(new { message = "Nhận chuyến thành công" }) 
            : result.ToProblemDetails();
    }

    // 4. Tài xế từ chối chuyến (yêu cầu notePhat)
    [HttpPost("{id:guid}/driver-reject")]
    public async Task<IResult> DriverReject(Guid id, [FromQuery] Guid driverId, CancellationToken ct)
    {
        var result = await sender.Send(new DriverRejectCommand(id, driverId), ct);
        return result.IsSuccess 
            ? Results.Ok(new { message = "Từ chối chuyến thành công" }) 
            : result.ToProblemDetails();
    }

    // 5. Tài xế đón khách
    [HttpPost("{id:guid}/pickup")]
    public async Task<IResult> PickUp(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new PickUpCommand(id), ct);
        return result.IsSuccess 
            ? Results.Ok(new { message = "Đã đón khách" }) 
            : result.ToProblemDetails();
    }

    // 6. Tài xế trả khách (kích hoạt thanh toán qua RabbitMQ)
    [HttpPost("{id:guid}/dropoff")]
    public async Task<IResult> DropOff(
        Guid id, 
        [FromQuery] decimal finalFare, 
        [FromQuery] PaymentMethod paymentMethod = PaymentMethod.Wallet, 
        CancellationToken ct = default)
    {
        var result = await sender.Send(new DropOffCommand(id, finalFare, paymentMethod), ct);
        return result.IsSuccess 
            ? Results.Ok(new { message = "Đã trả khách và gửi yêu cầu thanh toán" }) 
            : result.ToProblemDetails();
    }

    // 7. Hủy chuyến (yêu cầu notePhat)
    [HttpPost("{id:guid}/cancel")]
    public async Task<IResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new CancelTripCommand(id), ct);
        return result.IsSuccess 
            ? Results.Ok(new { message = "Hủy chuyến thành công" }) 
            : result.ToProblemDetails();
    }
}
