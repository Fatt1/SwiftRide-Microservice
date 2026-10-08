using Contracts.Domain;
using Payment.Domain.Enums;
using Shared.Enums.Payments;

namespace Payment.Domain.Entities;

public class LedgerEntry : EntityBase<Guid>
{
    public Guid PaymentId { get; private set; }
    /// <summary>
    /// Null for an external Card account, including a refund credit.
    /// </summary>
    public Guid? WalletId { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public EntryType EntryType { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation
    public PaymentTransaction Payment { get; private set; } = default!;
    public Wallet? Wallet { get; private set; }

    private LedgerEntry() { }

    public static LedgerEntry Create(
        Guid paymentId,
        Guid? walletId,
        PaymentMethod paymentMethod,
        EntryType entryType,
        decimal amount,
        string? description = null)
    {
        PaymentRules.Id(paymentId);
        PaymentRules.Amount(amount);
        PaymentRules.Require(Enum.IsDefined(paymentMethod) && Enum.IsDefined(entryType), "Invalid ledger type.");
        if (walletId.HasValue) PaymentRules.Id(walletId.Value);
        PaymentRules.Require(paymentMethod != PaymentMethod.Wallet || walletId.HasValue,
            "Wallet entries require a wallet id.");

        return new LedgerEntry
        {
            Id = Guid.CreateVersion7(),
            PaymentId = paymentId,
            WalletId = walletId,
            PaymentMethod = paymentMethod,
            EntryType = entryType,
            Amount = amount,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
