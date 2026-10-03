using Contracts.Domain;

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
        if (amount <= 0)
            throw new ArgumentException("Amount to credit must be greater than zero", nameof(amount));

        Balance += amount;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Debit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount to debit must be greater than zero", nameof(amount));

        if (Balance < amount)
            throw new InvalidOperationException($"Insufficient balance in wallet {Id}. Current balance: {Balance}, requested: {amount}");

        Balance -= amount;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }
}
