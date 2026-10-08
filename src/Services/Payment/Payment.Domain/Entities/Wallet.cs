using Contracts.Domain;
using Shared.Exceptions;

namespace Payment.Domain.Entities;

public class Wallet : EntityAuditableBase<Guid>
{
    public Guid UserId { get; private set; }
    public string UserRole { get; private set; } = default!; // 'rider' | 'driver'
    public decimal Balance { get; private set; }
    public string Currency { get; private set; } = "VND";

    // Navigation
    public ICollection<LedgerEntry> LedgerEntries { get; private set; } = [];

    private Wallet() { }

    public static Wallet Create(Guid userId, string userRole, decimal initialBalance = 0, string currency = "VND")
    {
        PaymentRules.Id(userId);
        PaymentRules.Currency(currency);
        PaymentRules.Amount(initialBalance, allowZero: true);
        PaymentRules.Require(!string.IsNullOrWhiteSpace(userRole), "Wallet role is required.");
        userRole = userRole.Trim().ToLowerInvariant();
        PaymentRules.Require(userRole is "rider" or "driver", "Invalid wallet role.");
        return new Wallet
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            UserRole = userRole.ToLowerInvariant(),
            Balance = initialBalance,
            Currency = currency,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow
        };
    }

    public void Credit(decimal amount)
    {
        PaymentRules.Amount(amount);

        Balance += amount;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Debit(decimal amount)
    {
        PaymentRules.Amount(amount);

        if (Balance < amount)
            throw new DomainException($"Insufficient balance in wallet {Id}. Current balance: {Balance}, requested: {amount}");

        Balance -= amount;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

}
