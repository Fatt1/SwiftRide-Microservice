using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Configurations;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.PaymentMethod)
              .HasConversion<string>()
              .HasMaxLength(10)
              .IsRequired();

        builder.Property(l => l.EntryType)
              .HasConversion<string>()
              .HasMaxLength(10)
              .IsRequired();

        builder.Property(l => l.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(l => l.Description);

        builder.HasOne(l => l.Wallet)
              .WithMany(w => w.LedgerEntries)
              .HasForeignKey(l => l.WalletId)
              .OnDelete(DeleteBehavior.Restrict)
              .IsRequired(false); // Nullable for external card debit
    }
}
