using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.TripId).IsUnique();
        builder.HasIndex(p => p.IdempotencyKey).IsUnique();

        builder.Property(p => p.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();

        builder.Property(p => p.Status)
              .HasConversion<string>()
              .HasMaxLength(20)
              .IsRequired();

        builder.Property(p => p.PaymentMethod)
              .HasConversion<string>()
              .HasMaxLength(10)
              .IsRequired();

        builder.Property(p => p.GatewayToken);
        builder.Property(p => p.GatewayResponse).HasColumnType("jsonb");
        builder.Property(p => p.FailureReason).HasMaxLength(500);

        builder.HasMany(p => p.LedgerEntries)
              .WithOne(l => l.Payment)
              .HasForeignKey(l => l.PaymentId)
              .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Refunds)
              .WithOne(r => r.Payment)
              .HasForeignKey(r => r.PaymentId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
