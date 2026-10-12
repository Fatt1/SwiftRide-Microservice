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
        logger.LogInformation("Nhận TripDropOffEvent: MessageId={MessageId}, TripId={TripId}, RiderId={RiderId}, DriverId={DriverId}, Amount={Amount} {Currency}, PaymentMethod={PaymentMethod}, RetryAttempt={RetryAttempt}",
            context.MessageId, message.TripId, message.RiderId, message.DriverId, message.Amount,
            message.Currency, message.PaymentMethod, context.GetRetryAttempt());
        var result = await sender.Send(new CreatePaymentCommand
        {
            TripId = message.TripId, RiderId = message.RiderId, DriverId = message.DriverId,
            Amount = message.Amount, Currency = message.Currency, PaymentMethod = message.PaymentMethod,
            GatewayToken = message.GatewayToken, IdempotencyKey = message.Id, CorrelationId = message.CorrelationId
        }, context.CancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning("Không xử lý được TripDropOffEvent: MessageId={MessageId}, TripId={TripId}, Reason={Reason}. Consumer sẽ ném lỗi; kiểm tra queue lỗi nếu hết retry. TraceId={TraceId} SpanId={SpanId}",
                context.MessageId, message.TripId, result.Error!.Message,
                Activity.Current?.TraceId.ToString(), Activity.Current?.SpanId.ToString());
            throw new ArgumentException(result.Error.Message);
        }
        logger.LogInformation("Xử lý xong TripDropOffEvent: MessageId={MessageId}, TripId={TripId}, PaymentId={PaymentId}, PaymentStatus={PaymentStatus}. Dữ liệu đã lưu; đang chờ transaction của consumer hoàn tất và outbox chuyển event kết quả sang RabbitMQ",
            context.MessageId, message.TripId, result.Value.PaymentId, result.Value.Status);
    }
}
