using System.Diagnostics;
using EventBus.Messages.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Features.Payments.Commands.CreatePayment;

namespace Payment.Infrastructure.Messaging;

public sealed class TripDropOffConsumer(ISender sender, ILogger<TripDropOffConsumer> logger) : IConsumer<TripDropOffEvent>
{
    public async Task Consume(ConsumeContext<TripDropOffEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Đã nhận message TripDropOff {MessageId} cho chuyến {TripId}, phương thức {PaymentMethod}, lần thử lại {RetryAttempt}",
            context.MessageId, message.TripId, message.PaymentMethod, context.GetRetryAttempt());
        var result = await sender.Send(new CreatePaymentCommand
        {
            TripId = message.TripId, RiderId = message.RiderId, DriverId = message.DriverId,
            Amount = message.Amount, Currency = message.Currency, PaymentMethod = message.PaymentMethod,
            GatewayToken = message.GatewayToken, IdempotencyKey = message.Id, CorrelationId = message.CorrelationId
        }, context.CancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning("Từ chối xử lý kết thúc chuyến {TripId}: {Reason}. TraceId={TraceId} SpanId={SpanId}",
                message.TripId, result.Error!.Message, Activity.Current?.TraceId.ToString(), Activity.Current?.SpanId.ToString());
            throw new ArgumentException(result.Error.Message);
        }
        logger.LogInformation("Đã xử lý TripDropOff cho chuyến {TripId}; thay đổi thanh toán đã sẵn sàng, đang chờ consumer commit transaction", message.TripId);
    }
}
