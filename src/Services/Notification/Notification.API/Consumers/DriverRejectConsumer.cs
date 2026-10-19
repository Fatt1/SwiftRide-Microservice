using EventBus.Messages.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.API.Hubs;

namespace Notification.API.Consumers;

public class DriverRejectConsumer : IConsumer<DriverRejectEvent>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<DriverRejectConsumer> _logger;

    public DriverRejectConsumer(IHubContext<NotificationHub> hubContext, ILogger<DriverRejectConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DriverRejectEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing DriverRejectEvent for TripId={TripId}, DriverId={DriverId}, Reason={Reason}", msg.TripId, msg.DriverId, msg.Reason);

        var payload = new
        {
            tripId = msg.TripId,
            driverId = msg.DriverId,
            riderId = msg.RiderId,
            reason = msg.Reason,
            status = "DriverRejected",
            timestamp = DateTime.UtcNow
        };

        // Broadcast to specific event name 'DriverRejected'
        await _hubContext.Clients.All.SendAsync("DriverRejected", payload);

        // Also broadcast to generic 'ReceiveNotification'
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
        {
            type = "DriverRejected",
            title = "Tài xế đã từ chối chuyến xe",
            message = $"Tài xế đã từ chối nhận chuyến. Lý do: {msg.Reason}",
            data = payload,
            timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("Successfully broadcasted DriverRejected notification to WebSocket clients.");
    }
}
