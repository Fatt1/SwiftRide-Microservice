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
           
            var byTrip = await payments.GetByTripIdAsync(request.TripId, ct);
            var byKey = await payments.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
            var existing = byTrip ?? byKey;
            if (existing is not null)
            {
                if ((byTrip is not null && byKey is not null && byTrip.Id != byKey.Id) ||
                    existing.TripId != request.TripId)
                {
                    logger.LogWarning("Yêu cầu thanh toán xung đột với thanh toán {PaymentId} của chuyến {TripId}", existing.Id, request.TripId);
                    return Result.Failure<PaymentDto>(new ConflictError("The trip or idempotency key belongs to a different payment request."));
                }
                logger.LogInformation("Thanh toán trùng: TripId={TripId}, PaymentId={PaymentId}, Status={Status}, IdempotencyKey={IdempotencyKey}; trả kết quả cũ, không thu tiền lần nữa",
                    request.TripId, existing.Id, existing.Status, request.IdempotencyKey);
                return Result.Success(PaymentDto.From(existing));
            }

            await transactions.LockWalletsAsync([request.RiderId, request.DriverId], ct);
            var payment = PaymentTransaction.Create(request.TripId, request.RiderId, request.DriverId,
                request.Amount, request.PaymentMethod, request.IdempotencyKey, request.GatewayToken, request.Currency);
            var context = new PaymentExecutionContext(payment);
            logger.LogInformation("Đang thu tiền bằng {PaymentMethod} cho thanh toán {PaymentId}", payment.PaymentMethod, payment.Id);
            var outcome = await strategies.GetStrategy(request.PaymentMethod).ChargeAsync(context, ct);
            payments.Add(payment);
            var correlationId = request.CorrelationId == Guid.Empty ? request.IdempotencyKey : request.CorrelationId;
            if (outcome.IsSuccess)
            {
                payments.AddLedgerEntries([
                    LedgerEntry.Create(payment.Id, context.DebitWalletId, payment.PaymentMethod,
                        EntryType.Debit, payment.Amount, "Payment charge"),
                    LedgerEntry.Create(payment.Id, context.CreditWalletId, payment.PaymentMethod,
                        EntryType.Credit, payment.Amount, "Driver earnings")]);
                payment.MarkCompleted(context.ProcessorResponse);
                await publisher.Publish(new PaymentCompletedEvent
                {
                    CorrelationId = correlationId, PaymentId = payment.Id, TripId = payment.TripId,
                    RiderId = payment.RiderId, DriverId = payment.DriverId, Amount = payment.Amount,
                    PaymentMethod = payment.PaymentMethod
                }, ct);
                logger.LogInformation("Thu tiền thành công: TripId={TripId}, PaymentId={PaymentId}, Amount={Amount} {Currency}, Method={PaymentMethod}; đã tạo hai dòng ledger và đưa PaymentCompletedEvent vào outbox, chờ lưu transaction",
                    payment.TripId, payment.Id, payment.Amount, payment.Currency, payment.PaymentMethod);
            }
            else
            {
                payment.MarkFailed(outcome.Error!.Message, context.ProcessorResponse);
                await publisher.Publish(new PaymentFailedEvent
                {
                    CorrelationId = correlationId, TripId = payment.TripId, RiderId = payment.RiderId,
                    Amount = payment.Amount, Reason = payment.FailureReason!
                }, ct);
                logger.LogWarning("Thu tiền thất bại: TripId={TripId}, PaymentId={PaymentId}, Method={PaymentMethod}, Reason={Reason}; PaymentStatus=Failed, không tạo ledger chuyển tiền, đã đưa PaymentFailedEvent vào outbox, chờ lưu transaction",
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
