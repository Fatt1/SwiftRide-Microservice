using Contracts.Domain;
using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

public class LedgerEntry : EntityBase<Guid>
{
    public Guid PaymentId { get; private set; }
    /// <summary>
    /// Nullable: NULL when debiting directly from an external card via mock gateway.
    /// Non-null when debiting from a wallet or crediting to any wallet.
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
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        // Validation rule: Debit with wallet method MUST have walletId
        if (entryType == EntryType.Debit && paymentMethod == PaymentMethod.Wallet && !walletId.HasValue)
            throw new ArgumentException("WalletId cannot be null when debiting from a wallet", nameof(walletId));

        // Credit to driver ALWAYS requires walletId
        if (entryType == EntryType.Credit && !walletId.HasValue)
            throw new ArgumentException("WalletId cannot be null for credit entries", nameof(walletId));

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
