using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasKey(w => w.Id);
        builder.HasIndex(w => w.UserId).IsUnique();

        builder.Property(w => w.UserRole).HasMaxLength(10).IsRequired();
        builder.Property(w => w.Balance).HasPrecision(18, 2).IsRequired();
        builder.Property(w => w.Currency).HasMaxLength(3).IsRequired();
    }
}
