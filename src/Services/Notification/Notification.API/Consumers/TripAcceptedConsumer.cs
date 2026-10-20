using EventBus.Messages.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.API.Hubs;

namespace Notification.API.Consumers;

public class TripAcceptedConsumer : IConsumer<TripAcceptedEvent>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<TripAcceptedConsumer> _logger;

    public TripAcceptedConsumer(IHubContext<NotificationHub> hubContext, ILogger<TripAcceptedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TripAcceptedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing TripAcceptedEvent for TripId={TripId}, DriverId={DriverId}", msg.TripId, msg.DriverId);

        var payload = new
        {
            tripId = msg.TripId,
            driverId = msg.DriverId,
            driverName = msg.DriverName ?? "Tài xế",
            riderId = msg.RiderId,
            status = "DriverAccepted",
            timestamp = DateTime.UtcNow
        };

        // Broadcast to specific event name 'TripAccepted'
        await _hubContext.Clients.All.SendAsync("TripAccepted", payload);

        // Also broadcast to generic 'ReceiveNotification'
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
        {
            type = "TripAccepted",
            title = "Tài xế đã chấp nhận chuyến xe!",
            message = $"Tài xế {msg.DriverName ?? "đã"} nhận chuyến xe của bạn và đang trên đường đến đón.",
            data = payload,
            timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("Successfully broadcasted TripAccepted notification to WebSocket clients.");
    }
}
