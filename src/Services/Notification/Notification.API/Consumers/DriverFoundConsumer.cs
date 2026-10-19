using EventBus.Messages.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.API.Hubs;

namespace Notification.API.Consumers;

public class DriverFoundConsumer : IConsumer<DriverFoundEvent>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<DriverFoundConsumer> _logger;

    public DriverFoundConsumer(IHubContext<NotificationHub> hubContext, ILogger<DriverFoundConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DriverFoundEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing DriverFoundEvent for TripId={TripId}, DriverId={DriverId}", msg.TripId, msg.DriverId);

        var payload = new
        {
            tripId = msg.TripId,
            driverId = msg.DriverId,
            driverName = msg.DriverName,
            riderId = msg.RiderId,
            pickupAddress = msg.PickupAddress,
            dropoffAddress = msg.DropoffAddress,
            totalFare = msg.TotalFare,
            estimatedDurationMinutes = msg.EstimatedDurationMinutes,
            distanceKm = msg.DistanceKm,
            timestamp = DateTime.UtcNow
        };

        // Broadcast to specific event name 'DriverFound'
        await _hubContext.Clients.All.SendAsync("DriverFound", payload);

        // Also broadcast to generic 'ReceiveNotification' for universal listeners
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
        {
            type = "DriverFound",
            title = "Có chuyến xe mới dành cho bạn!",
            message = $"Từ: {msg.PickupAddress} -> Đến: {msg.DropoffAddress} | Cước phí: {msg.TotalFare:N0} đ | Ước tính: {msg.EstimatedDurationMinutes} phút",
            data = payload,
            timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("Successfully broadcasted DriverFound notification to WebSocket clients.");
    }
}
