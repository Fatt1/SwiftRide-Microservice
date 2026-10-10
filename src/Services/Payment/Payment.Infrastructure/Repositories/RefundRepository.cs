using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Repositories;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public sealed class RefundRepository(PaymentDbContext context)
    : PaymentRepositoryBase<Refund>(context), IRefundRepository
{
    public Task<Refund?> GetCompletedByPaymentIdAsync(Guid paymentId, CancellationToken ct) =>
        FindByCondition(x => x.PaymentId == paymentId && x.Status == RefundStatus.Completed,
            trackChanges: true).SingleOrDefaultAsync(ct);
    public void Add(Refund refund) => Stage(refund);
}
