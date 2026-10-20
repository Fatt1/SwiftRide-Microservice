using Common.Logging;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.SignalR;
using Notification.API.Consumers;
using Notification.API.Hubs;
using Serilog;
using Shared.Exceptions;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Serilog logging
builder.Host.UseSerilog(Serilogger.Configure);

// OpenAPI
builder.Services.AddOpenApi("v1");
builder.Services.AddExceptionHandler<GlobalExceptionHandlerMiddleware>();
builder.Services.AddProblemDetails();

// CORS for WebSocket / SignalR (supports Vite dev server, React, etc.)
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRCors", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// MassTransit with RabbitMQ & Event Consumers
builder.Services.AddCustomMassTransit(builder.Configuration, "notification", busConfig =>
{
    busConfig.AddConsumer<DriverFoundConsumer>();
    busConfig.AddConsumer<TripAcceptedConsumer>();
    busConfig.AddConsumer<DriverRejectConsumer>();
});

var app = builder.Build();

app.UseExceptionHandler();

// OpenAPI & Scalar in Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapCustomScalarApiReference();
}

app.UseCors("SignalRCors");

// SignalR WebSocket Hub Endpoints
app.MapHub<NotificationHub>("/hubs/notification");
app.MapHub<NotificationHub>("/notifications"); // alias for convenience

// Info endpoint
app.MapGet("/", () => Results.Ok(new
{
    Service = "SwiftRide Notification Service (SignalR WebSocket)",
    Status = "Healthy",
    Version = "1.0",
    HubUrl = "/hubs/notification",
    Timestamp = DateTime.UtcNow
}));

// Test endpoint to trigger mock notifications directly
app.MapPost("/api/v1/notifications/test", async (
    string type,
    Microsoft.AspNetCore.SignalR.IHubContext<NotificationHub> hubContext) =>
{
    var samplePayload = new
    {
        tripId = Guid.NewGuid(),
        driverId = Guid.NewGuid(),
        driverName = "Nguyễn Văn Tài Xế",
        riderId = Guid.NewGuid(),
        pickupAddress = "Chợ Bến Thành, Quận 1",
        dropoffAddress = "Landmark 81, Bình Thạnh",
        totalFare = 68000m,
        estimatedDurationMinutes = 15.0,
        distanceKm = 5.2,
        reason = "Tài xế bận việc đột xuất",
        timestamp = DateTime.UtcNow
    };

    switch (type.ToUpperInvariant())
    {
        case "DRIVERFOUND":
            await hubContext.Clients.All.SendAsync("DriverFound", samplePayload);
            await hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                type = "DriverFound",
                title = "Có chuyến xe mới dành cho bạn!",
                message = "Từ: Chợ Bến Thành -> Landmark 81 | Cước: 68.000 đ",
                data = samplePayload
            });
            break;
        case "TRIPACCEPTED":
            await hubContext.Clients.All.SendAsync("TripAccepted", samplePayload);
            await hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                type = "TripAccepted",
                title = "Tài xế đã nhận chuyến!",
                message = "Tài xế Nguyễn Văn Tài Xế đã nhận chuyến xe.",
                data = samplePayload
            });
            break;
        case "DRIVERREJECTED":
            await hubContext.Clients.All.SendAsync("DriverRejected", samplePayload);
            await hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                type = "DriverRejected",
                title = "Tài xế đã từ chối!",
                message = "Tài xế đã từ chối chuyến đi.",
                data = samplePayload
            });
            break;
        default:
            return Results.BadRequest(new { error = "Loại type không hợp lệ. Chọn: driverfound, tripaccepted, driverrejected" });
    }

    return Results.Ok(new { message = $"Đã bắn sự kiện test '{type}' tới WebSocket thành công!" });
});

await app.RunAsync();
