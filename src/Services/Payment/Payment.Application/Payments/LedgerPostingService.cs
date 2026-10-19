using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Repositories;
using Shared.Enums.Payments;

namespace Payment.Application.Payments;

public sealed class LedgerPostingService(ILedgerRepository ledger)
{
    public void Record(Guid paymentId, Wallet? wallet, PaymentMethod method,
        EntryType entryType, decimal amount, string description)
    {
        var entry = LedgerEntry.Create(paymentId, wallet?.Id, method, entryType, amount, description);
        if (wallet is not null)
        {
            switch (entryType)
            {
                case EntryType.Debit:
                    wallet.Debit(amount);
                    break;
                case EntryType.Credit:
                    wallet.Credit(amount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(entryType));
            }
        }
        ledger.Add(entry);
    }

    public void RecordTopUp(Guid idempotencyKey, Guid paymentId, Wallet wallet, decimal amount)
    {
        var entry = LedgerEntry.CreateTopUp(idempotencyKey, paymentId, wallet.Id, amount);
        wallet.Credit(amount);
        ledger.Add(entry);
    }
}
