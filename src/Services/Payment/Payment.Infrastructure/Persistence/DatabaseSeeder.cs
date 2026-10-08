using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(PaymentDbContext context, ILogger logger)
    {
        var rider = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var driver = Guid.Parse("22222222-2222-2222-2222-222222222222");
        if (!await context.Wallets.AnyAsync(w => w.UserId == rider))
            context.Wallets.Add(Wallet.Create(rider, "rider", 200000));
        if (!await context.Wallets.AnyAsync(w => w.UserId == driver))
            context.Wallets.Add(Wallet.Create(driver, "driver"));
        await context.SaveChangesAsync();
        logger.LogInformation("Payment demo wallets are available.");
    }
}
