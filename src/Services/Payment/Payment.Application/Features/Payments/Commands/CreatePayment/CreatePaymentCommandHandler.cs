using System.Diagnostics;
using EventBus.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions;
using Payment.Application.Dtos;
using Payment.Application.Payments.Contexts;
using Payment.Domain.Entities;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Payments.Commands.CreatePayment;

public sealed class CreatePaymentCommandHandler(
    IPaymentRepository payments, IPaymentTransaction transactions, IPaymentStrategyFactory strategies,
    IPublishEndpoint publisher, ILogger<CreatePaymentCommandHandler> logger)
    : ICommandHandler<CreatePaymentCommand, PaymentDto>
{
    public async Task<Result<PaymentDto>> Handle(CreatePaymentCommand request, CancellationToken ct)
    {
        await using var transaction = await transactions.BeginOwnedTransactionAsync();
        try
        {
            await transactions.LockPaymentsAsync([request.TripId, request.IdempotencyKey], ct);
            logger.LogInformation("Bắt đầu xử lý thanh toán: TripId={TripId}, RiderId={RiderId}, DriverId={DriverId}, Amount={Amount} {Currency}, PaymentMethod={PaymentMethod}, IdempotencyKey={IdempotencyKey}, RetryAttempt={RetryAttempt}",
                request.TripId, request.RiderId, request.DriverId, request.Amount, request.Currency,
                request.PaymentMethod, request.IdempotencyKey, request.RetryAttempt);

            // 1. Kiểm tra trùng lặp theo IdempotencyKey (cùng key trả lại kết quả cũ)
            var existingByKey = await payments.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
            if (existingByKey is not null)
            {
                if (existingByKey.TripId != request.TripId ||
                    existingByKey.RiderId != request.RiderId ||
                    existingByKey.DriverId != request.DriverId || existingByKey.Currency != request.Currency ||
                    existingByKey.Amount != request.Amount ||
                    existingByKey.PaymentMethod != request.PaymentMethod)
                {
                    logger.LogWarning("IdempotencyKey {Key} xung đột dữ liệu với thanh toán {PaymentId}", request.IdempotencyKey, existingByKey.Id);
                    return Result.Failure<PaymentDto>(new ConflictError("The idempotency key belongs to a different payment request."));
                }
                logger.LogWarning("Giao dịch trùng theo IdempotencyKey={Key}: trả kết quả cũ (PaymentId={PaymentId}, Status={Status})",
                    request.IdempotencyKey, existingByKey.Id, existingByKey.Status);
                return Result.Success(PaymentDto.From(existingByKey));
            }

            // The drop-off consumer can also receive an older event with a different key.
            var completedPayment = await payments.GetCompletedByTripIdAsync(request.TripId, ct);
            if (completedPayment is not null)
            {
                if (completedPayment.RiderId != request.RiderId || completedPayment.DriverId != request.DriverId ||
                    completedPayment.Amount != request.Amount || completedPayment.Currency != request.Currency)
                    return Result.Failure<PaymentDto>(new ConflictError("Trip is settled with different payment details."));

                return Result.Success(PaymentDto.From(completedPayment));
            }

            await transactions.LockWalletsAsync([request.RiderId, request.DriverId], ct);
            var payment = PaymentTransaction.Create(request.TripId, request.RiderId, request.DriverId,
                request.Amount, request.PaymentMethod, request.IdempotencyKey, request.GatewayToken, request.Currency);
            var context = new PaymentExecutionContext(payment) { RetryAttempt = request.RetryAttempt };
            logger.LogInformation("Đang thu tiền bằng {PaymentMethod} cho thanh toán mới {PaymentId} của chuyến {TripId}", payment.PaymentMethod, payment.Id, payment.TripId);

            var outcome = await strategies.GetStrategy(request.PaymentMethod).ChargeAsync(context, ct);
            payments.Add(payment);
            if (outcome.IsSuccess)
            {
                payment.MarkCompleted(context.ProcessorResponse);
                await publisher.Publish(new PaymentCompletedEvent
                {
                    CorrelationId = request.IdempotencyKey,
                    PaymentId = payment.Id,
                    TripId = payment.TripId!.Value,
                    RiderId = payment.RiderId,
                    DriverId = payment.DriverId!.Value,
                    Amount = payment.Amount,
                    PaymentMethod = payment.PaymentMethod
                }, ct);
                logger.LogInformation("Thu tiền thành công: TripId={TripId}, PaymentId={PaymentId}, Amount={Amount} {Currency}, Method={PaymentMethod}",
                    payment.TripId, payment.Id, payment.Amount, payment.Currency, payment.PaymentMethod);
            }
            else
            {
                payment.MarkFailed(outcome.Error!.Message, context.ProcessorResponse);
                await publisher.Publish(new PaymentFailedEvent
                {
                    CorrelationId = request.IdempotencyKey,
                    TripId = payment.TripId!.Value,
                    RiderId = payment.RiderId,
                    Amount = payment.Amount,
                    Reason = payment.FailureReason!
                }, ct);
                logger.LogWarning("Thu tiền thất bại: TripId={TripId}, PaymentId={PaymentId}, Method={PaymentMethod}, Reason={Reason}",
                    payment.TripId, payment.Id, payment.PaymentMethod, payment.FailureReason);
            }
            logger.LogInformation("Thanh toán {PaymentId} cho chuyến {TripId} đã sẵn sàng để lưu, trạng thái {Status}. TraceId={TraceId} SpanId={SpanId}",
                payment.Id, payment.TripId, payment.Status, Activity.Current?.TraceId.ToString(), Activity.Current?.SpanId.ToString());
            if (transaction is not null)
            {
                await transactions.EndTransactionAsync();
                logger.LogInformation("Đã commit payment transaction: TripId={TripId}, PaymentId={PaymentId}, Status={Status}; outbox sẽ chuyển event kết quả sang RabbitMQ",
                    payment.TripId, payment.Id, payment.Status);
            }
            else
            {
                await transactions.SaveChangesAsync();
                logger.LogInformation("Đã lưu payment và event trong transaction hiện tại: TripId={TripId}, PaymentId={PaymentId}, Status={Status}; outbox sẽ gửi event sau khi commit",
                    payment.TripId, payment.Id, payment.Status);
            }
            return Result.Success(PaymentDto.From(payment));
        }
        catch (TransientCardPaymentException exception)
        {
            logger.LogWarning("Lỗi tạm thời từ cổng thanh toán: TripId={TripId}, IdempotencyKey={IdempotencyKey}, RetryAttempt={RetryAttempt}, Reason={Reason}",
                request.TripId, request.IdempotencyKey, request.RetryAttempt, exception.Message);
            throw;
        }
    }
}
