using MassTransit;
using Microsoft.EntityFrameworkCore;
using Trip.Domain.Entities;

namespace Trip.Infrastructure.Persistence;

public class TripDbContext : DbContext
{
    public TripDbContext(DbContextOptions<TripDbContext> options) : base(options)
    {
    }

    public DbSet<Trip.Domain.Entities.Trip> Trips => Set<Trip.Domain.Entities.Trip>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit Transactional Outbox entities
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        // Trip configuration
        modelBuilder.Entity<Trip.Domain.Entities.Trip>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.PickupAddress).HasMaxLength(500).IsRequired();
            entity.Property(t => t.DropoffAddress).HasMaxLength(500).IsRequired();

            entity.Property(t => t.PickupLat).HasPrecision(10, 7);
            entity.Property(t => t.PickupLng).HasPrecision(10, 7);
            entity.Property(t => t.DropoffLat).HasPrecision(10, 7);
            entity.Property(t => t.DropoffLng).HasPrecision(10, 7);

            entity.Property(t => t.EstimatedFare).HasPrecision(10, 2);
            entity.Property(t => t.FinalFare).HasPrecision(10, 2);
            entity.Property(t => t.DistanceKm).HasPrecision(8, 2);

            entity.Property(t => t.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(t => t.QuoteId).HasMaxLength(50);
        });
    }
}
