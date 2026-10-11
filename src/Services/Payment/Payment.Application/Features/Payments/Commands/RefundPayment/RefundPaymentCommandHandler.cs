using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions;
using Payment.Application.Dtos;
using Payment.Application.Payments.Contexts;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Payments.Commands.RefundPayment;

public sealed class RefundPaymentCommandHandler(
    IPaymentRepository payments, IPaymentTransaction transactions, IRefundRepository refunds, IPaymentStrategyFactory strategies,
    ILogger<RefundPaymentCommandHandler> logger)
    : ICommandHandler<RefundPaymentCommand, RefundDto>
{
    public async Task<Result<RefundDto>> Handle(RefundPaymentCommand request, CancellationToken ct)
    {
        await using var transaction = await transactions.BeginOwnedTransactionAsync();
        try
        {
            await transactions.LockPaymentsAsync([request.PaymentId], ct);
            logger.LogInformation("Bắt đầu hoàn tiền cho thanh toán {PaymentId}", request.PaymentId);
            var payment = await payments.GetByIdAsync(request.PaymentId, ct);
            if (payment is null)
            {
                logger.LogWarning("Từ chối hoàn tiền: không tìm thấy thanh toán {PaymentId}", request.PaymentId);
                return Result.Failure<RefundDto>(new NotFoundError("Payment", request.PaymentId));
            }
            var existing = await refunds.GetCompletedByPaymentIdAsync(payment.Id, ct);
            if (existing is not null)
            {
                logger.LogInformation("Trả về khoản hoàn tiền đã có {RefundId} của thanh toán {PaymentId}; không hoàn tiền lại", existing.Id, payment.Id);
                return Result.Success(RefundDto.From(existing));
            }
            if (payment.Status != PaymentStatus.Completed)
            {
                logger.LogWarning("Từ chối hoàn tiền cho thanh toán {PaymentId}: trạng thái {Status} không hợp lệ", payment.Id, payment.Status);
                return Result.Failure<RefundDto>(new ConflictError("Only a completed payment can be refunded."));
            }

            await transactions.LockWalletsAsync([payment.RiderId, payment.DriverId], ct);
            var context = new RefundExecutionContext(payment);
            logger.LogInformation("Đang hoàn tiền bằng {PaymentMethod} cho thanh toán {PaymentId}", payment.PaymentMethod, payment.Id);
            var outcome = await strategies.GetStrategy(payment.PaymentMethod).RefundAsync(context, ct);
            if (outcome.IsFailure)
            {
                logger.LogWarning("Từ chối hoàn tiền cho thanh toán {PaymentId}: {Reason}", payment.Id, outcome.Error!.Message);
                return Result.Failure<RefundDto>(outcome.Error!);
            }

            var refund = Refund.Create(payment.Id, payment.Amount, request.Reason);
            payments.AddLedgerEntries([
                LedgerEntry.Create(payment.Id, context.DebitWalletId, payment.PaymentMethod,
                    EntryType.Debit, payment.Amount, "Refund: reverse driver earnings"),
                LedgerEntry.Create(payment.Id, context.CreditWalletId, payment.PaymentMethod,
                    EntryType.Credit, payment.Amount, "Refund: return rider payment")]);
            refund.MarkCompleted();
            refunds.Add(refund);
            payment.MarkRefunded();
            logger.LogInformation("Khoản hoàn tiền {RefundId} của thanh toán {PaymentId} đã sẵn sàng để lưu. TraceId={TraceId} SpanId={SpanId}",
                refund.Id, payment.Id, Activity.Current?.TraceId.ToString(), Activity.Current?.SpanId.ToString());
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
            return Result.Success(RefundDto.From(refund));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Lỗi xử lý hoàn tiền");
            if (transaction is not null)
                await transactions.RollBackTransactionAsync();
            throw;
        }
    }
}
