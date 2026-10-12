using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Enums.Payments;
using Shared.Extensions;
using Trip.Application.Features.Trips.Commands.CancelTrip;
using Trip.Application.Features.Trips.Commands.CreateTrip;
using Trip.Application.Features.Trips.Commands.DriverAccept;
using Trip.Application.Features.Trips.Commands.DriverReject;
using Trip.Application.Features.Trips.Commands.DropOff;
using Trip.Application.Features.Trips.Commands.PickUp;
using Trip.Application.Features.Trips.Queries.GetTripById;

namespace Trip.API.Endpoints;

public static class TripEndpoints
{
    public static IEndpointRouteBuilder MapTripEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapApiV1Group("trips");

        // 1. Đặt chuyến xe mới
        group.MapPost("/", async ([FromBody] CreateTripCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess 
                ? Results.Ok(new { TripId = result.Value }) 
                : result.ToProblemDetails();
        });

        // 2. Lấy thông tin chuyến đi theo ID
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTripByIdQuery(id), ct);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemDetails();
        });

        // 3. Tài xế nhận chuyến
        group.MapPost("/{id:guid}/driver-accept", async (Guid id, [FromQuery] Guid driverId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DriverAcceptCommand(id, driverId), ct);
            return result.IsSuccess 
                ? Results.Ok(new { message = "Nhận chuyến thành công" }) 
                : result.ToProblemDetails();
        });

        // 4. Tài xế từ chối chuyến (theo notePhat)
        group.MapPost("/{id:guid}/driver-reject", async (Guid id, [FromQuery] Guid driverId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DriverRejectCommand(id, driverId), ct);
            return result.IsSuccess 
                ? Results.Ok(new { message = "Từ chối chuyến thành công" }) 
                : result.ToProblemDetails();
        });

        // 5. Tài xế đón khách
        group.MapPost("/{id:guid}/pickup", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new PickUpCommand(id), ct);
            return result.IsSuccess 
                ? Results.Ok(new { message = "Đã đón khách" }) 
                : result.ToProblemDetails();
        });

        // 6. Tài xế trả khách (kích hoạt thanh toán)
        group.MapPost("/{id:guid}/dropoff", async (
            Guid id, 
            [FromQuery] decimal finalFare, 
            [FromQuery] PaymentMethod paymentMethod = PaymentMethod.Wallet, 
            ISender sender = default!, 
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new DropOffCommand(id, finalFare, paymentMethod), ct);
            return result.IsSuccess 
                ? Results.Ok(new { message = "Đã trả khách và gửi yêu cầu thanh toán" }) 
                : result.ToProblemDetails();
        });

        // 7. Hủy chuyến (theo notePhat)
        group.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CancelTripCommand(id), ct);
            return result.IsSuccess 
                ? Results.Ok(new { message = "Hủy chuyến thành công" }) 
                : result.ToProblemDetails();
        });

        return endpoints;
    }
}
