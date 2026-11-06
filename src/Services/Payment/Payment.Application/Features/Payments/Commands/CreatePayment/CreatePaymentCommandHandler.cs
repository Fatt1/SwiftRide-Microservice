using System.Diagnostics;
using EventBus.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions;
using Payment.Application.Dtos;
using Payment.Application.Payments.Contexts;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
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
            logger.LogInformation("Bắt đầu xử lý thanh toán: TripId={TripId}, RiderId={RiderId}, DriverId={DriverId}, Amount={Amount} {Currency}, PaymentMethod={PaymentMethod}, IdempotencyKey={IdempotencyKey}",
                request.TripId, request.RiderId, request.DriverId, request.Amount, request.Currency,
                request.PaymentMethod, request.IdempotencyKey);
           
            // 1. Kiểm tra trùng lặp theo IdempotencyKey (cùng key trả lại kết quả cũ)
            var existingByKey = await payments.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
            if (existingByKey is not null)
            {
                if (existingByKey.TripId != request.TripId ||
                    existingByKey.RiderId != request.RiderId ||
                    existingByKey.Amount != request.Amount ||
                    existingByKey.PaymentMethod != request.PaymentMethod)
                {
                    logger.LogWarning("IdempotencyKey {Key} xung đột dữ liệu với thanh toán {PaymentId}", request.IdempotencyKey, existingByKey.Id);
                    return Result.Failure<PaymentDto>(new ConflictError("The idempotency key belongs to a different payment request."));
                }
                logger.LogInformation("Giao dịch trùng theo IdempotencyKey={Key}: trả kết quả cũ (PaymentId={PaymentId}, Status={Status})",
                    request.IdempotencyKey, existingByKey.Id, existingByKey.Status);
                return Result.Success(PaymentDto.From(existingByKey));
            }

            // 2. Kiểm tra nếu chuyến đi này ĐÃ có giao dịch hoàn tất thành công (Completed)
            var completedPayment = await payments.GetCompletedByTripIdAsync(request.TripId, ct);
            if (completedPayment is not null)
            {
                logger.LogInformation("Chuyến đi {TripId} đã được thanh toán thành công trước đó (PaymentId={PaymentId})",
                    request.TripId, completedPayment.Id);
                return Result.Success(PaymentDto.From(completedPayment));
            }

            // 3. Cho phép 1 chuyến đi có nhiều PaymentTransactions (khi thanh toán lại là tạo mới hoàn toàn)
            await transactions.LockWalletsAsync([request.RiderId, request.DriverId], ct);
            var payment = PaymentTransaction.Create(request.TripId, request.RiderId, request.DriverId,
                request.Amount, request.PaymentMethod, request.IdempotencyKey, request.GatewayToken, request.Currency);
            var context = new PaymentExecutionContext(payment) { RetryAttempt = request.RetryAttempt };
            logger.LogInformation("Đang thu tiền bằng {PaymentMethod} cho thanh toán mới {PaymentId} của chuyến {TripId}", payment.PaymentMethod, payment.Id, payment.TripId);
            
            var outcome = await strategies.GetStrategy(request.PaymentMethod).ChargeAsync(context, ct);
            payments.Add(payment);
            var correlationId = request.CorrelationId == Guid.Empty ? request.IdempotencyKey : request.CorrelationId;
            if (outcome.IsSuccess)
            {
                payment.MarkCompleted(context.ProcessorResponse);
                await publisher.Publish(new PaymentCompletedEvent
                {
                    CorrelationId = correlationId, PaymentId = payment.Id, TripId = payment.TripId!.Value,
                    RiderId = payment.RiderId, DriverId = payment.DriverId!.Value, Amount = payment.Amount,
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
                    CorrelationId = correlationId, TripId = payment.TripId!.Value, RiderId = payment.RiderId,
                    Amount = payment.Amount, Reason = payment.FailureReason!
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
                logger.LogInformation("Đã lưu payment và event vào transaction của consumer: TripId={TripId}, PaymentId={PaymentId}, Status={Status}; chờ consumer commit rồi outbox mới chuyển event sang RabbitMQ",
                    payment.TripId, payment.Id, payment.Status);
            }
            return Result.Success(PaymentDto.From(payment));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Lỗi xử lý thanh toán: TripId={TripId}, IdempotencyKey={IdempotencyKey}, PaymentMethod={PaymentMethod}; transaction do handler sở hữu sẽ rollback nếu có",
                request.TripId, request.IdempotencyKey, request.PaymentMethod);
            if (transaction is not null)
                await transactions.RollBackTransactionAsync();
            throw;
        }
    }
}
