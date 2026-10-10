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
    IPaymentRepository payments, IRefundRepository refunds, IPaymentStrategyFactory strategies,
    IPaymentUnitOfWork unitOfWork, ILogger<RefundPaymentCommandHandler> logger)
    : ICommandHandler<RefundPaymentCommand, RefundDto>
{
    public Task<Result<RefundDto>> Handle(RefundPaymentCommand request, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync([request.PaymentId], async ct =>
        {
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

            await unitOfWork.LockWalletsAsync([payment.RiderId, payment.DriverId], ct);
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
            return Result.Success(RefundDto.From(refund));
        }, cancellationToken);
}
