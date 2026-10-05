using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trip.Infrastructure.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip.Domain.Entities.Trip>
{
    public void Configure(EntityTypeBuilder<Trip.Domain.Entities.Trip> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.PickupAddress).HasMaxLength(500).IsRequired();
        builder.Property(t => t.DropoffAddress).HasMaxLength(500).IsRequired();

        builder.Property(t => t.PickupLat).HasPrecision(10, 7);
        builder.Property(t => t.PickupLng).HasPrecision(10, 7);
        builder.Property(t => t.DropoffLat).HasPrecision(10, 7);
        builder.Property(t => t.DropoffLng).HasPrecision(10, 7);

        builder.Property(t => t.EstimatedFare).HasPrecision(10, 2);
        builder.Property(t => t.FinalFare).HasPrecision(10, 2);
        builder.Property(t => t.DistanceKm).HasPrecision(8, 2);

        builder.Property(t => t.Status)
              .HasConversion<string>()
              .HasMaxLength(30)
              .IsRequired();

        builder.Property(t => t.QuoteId).HasMaxLength(50);
        builder.Property(t => t.PaymentFailureReason).HasMaxLength(500);
    }
}
