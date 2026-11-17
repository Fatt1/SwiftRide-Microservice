using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Trip.Infrastructure.Persistence;
namespace Trip.API;
// EF tooling must not start the application, broker, or run startup migrations.
public sealed class TripDbContextFactory : IDesignTimeDbContextFactory<TripDbContext>
{
    public TripDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<TripDbContext>()
        .UseSqlServer("Server=localhost,1435;Database=tripdb;User Id=sa;Password=Password@123;TrustServerCertificate=True;").Options);
}
