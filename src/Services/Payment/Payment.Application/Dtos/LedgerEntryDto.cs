using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Shared.Enums.Payments;

namespace Payment.Application.Dtos;

public sealed record LedgerEntryDto(Guid Id, Guid? PaymentId, Guid? WalletId,
    PaymentMethod PaymentMethod, EntryType EntryType, decimal Amount, string? Description,
    DateTimeOffset CreatedAt)
{
    public static LedgerEntryDto From(LedgerEntry entry) => new(entry.Id, entry.PaymentId,
        entry.WalletId, entry.PaymentMethod, entry.EntryType, entry.Amount, entry.Description,
        entry.CreatedAt);
}
