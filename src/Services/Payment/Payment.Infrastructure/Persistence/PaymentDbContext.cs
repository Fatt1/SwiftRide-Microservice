using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<PaymentTransaction> Payments => Set<PaymentTransaction>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Refund> Refunds => Set<Refund>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit Transactional Outbox entities
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        // Wallets
        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => w.UserId).IsUnique();

            entity.Property(w => w.UserRole).HasMaxLength(10).IsRequired();
            entity.Property(w => w.Balance).HasPrecision(18, 2).IsRequired();
            entity.Property(w => w.Currency).HasMaxLength(3).IsRequired();
        });

        // Payments
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.TripId).IsUnique();
            entity.HasIndex(p => p.IdempotencyKey).IsUnique();

            entity.Property(p => p.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(p => p.Currency).HasMaxLength(3).IsRequired();

            entity.Property(p => p.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(p => p.PaymentMethod)
                  .HasConversion<string>()
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(p => p.GatewayToken);
            entity.Property(p => p.GatewayResponse).HasColumnType("jsonb");

            entity.HasMany(p => p.LedgerEntries)
                  .WithOne(l => l.Payment)
                  .HasForeignKey(l => l.PaymentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Refunds)
                  .WithOne(r => r.Payment)
                  .HasForeignKey(r => r.PaymentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // LedgerEntries
        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.Property(l => l.PaymentMethod)
                  .HasConversion<string>()
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(l => l.EntryType)
                  .HasConversion<string>()
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(l => l.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(l => l.Description);

            entity.HasOne(l => l.Wallet)
                  .WithMany(w => w.LedgerEntries)
                  .HasForeignKey(l => l.WalletId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false); // Nullable for external card debit
        });

        // Refunds
        modelBuilder.Entity<Refund>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(r => r.Reason).IsRequired();

            entity.Property(r => r.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();
        });
    }
}
