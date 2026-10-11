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
            logger.LogInformation("Bắt đầu thanh toán cho chuyến {TripId}, phương thức {PaymentMethod}, khóa chống xử lý trùng {IdempotencyKey}",
                request.TripId, request.PaymentMethod, request.IdempotencyKey);
           
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
                logger.LogInformation("Trả về thanh toán đã có {PaymentId}, trạng thái {Status}; không thu tiền lại", existing.Id, existing.Status);
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
                logger.LogInformation("Đã chấp nhận thu tiền cho thanh toán {PaymentId}; chuẩn bị lưu sổ cái và sự kiện hoàn tất vào outbox", payment.Id);
                await publisher.Publish(new PaymentCompletedEvent
                {
                    CorrelationId = correlationId, PaymentId = payment.Id, TripId = payment.TripId,
                    RiderId = payment.RiderId, DriverId = payment.DriverId, Amount = payment.Amount,
                    PaymentMethod = payment.PaymentMethod
                }, ct);
            }
            else
            {
                payment.MarkFailed(outcome.Error!.Message, context.ProcessorResponse);
                logger.LogWarning("Từ chối thu tiền cho thanh toán {PaymentId}: {Reason}; chuẩn bị lưu sự kiện thất bại vào outbox", payment.Id, payment.FailureReason);
                await publisher.Publish(new PaymentFailedEvent
                {
                    CorrelationId = correlationId, TripId = payment.TripId, RiderId = payment.RiderId,
                    Amount = payment.Amount, Reason = payment.FailureReason!
                }, ct);
            }
            logger.LogInformation("Thanh toán {PaymentId} cho chuyến {TripId} đã sẵn sàng để lưu, trạng thái {Status}. TraceId={TraceId} SpanId={SpanId}",
                payment.Id, payment.TripId, payment.Status, Activity.Current?.TraceId.ToString(), Activity.Current?.SpanId.ToString());
            if (transaction is not null)
            {
                await transactions.EndTransactionAsync();
                logger.LogInformation("Đã lưu dữ liệu và commit transaction");
            }
            else
            {
                await transactions.SaveChangesAsync();
                logger.LogInformation("Đã lưu dữ liệu; đang chờ consumer commit transaction");
            }
            return Result.Success(PaymentDto.From(payment));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Lỗi xử lý thanh toán");
            if (transaction is not null)
                await transactions.RollBackTransactionAsync();
            throw;
        }
    }
}
