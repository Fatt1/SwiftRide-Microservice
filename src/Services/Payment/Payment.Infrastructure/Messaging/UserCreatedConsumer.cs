using EventBus.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Payment.Domain.Entities;
using Payment.Domain.Repositories;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Messaging;

public sealed class UserCreatedConsumer(
    IWalletRepository wallets,
    PaymentDbContext dbContext,
    ILogger<UserCreatedConsumer> logger) : IConsumer<UserCreated>
{
    public async Task Consume(ConsumeContext<UserCreated> context)
    {
        if (!Guid.TryParse(context.Message.UserId, out var userId) || userId == Guid.Empty)
            throw new ArgumentException($"Invalid UserId in UserCreated event: {context.Message.UserId}");

        if (await wallets.GetByUserIdAsync(userId, context.CancellationToken) is not null)
        {
            logger.LogInformation("Wallet already exists for user {UserId}", userId);
            return;
        }

        wallets.Add(Wallet.Create(userId));
        await dbContext.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Created wallet for user {UserId}", userId);
    }
}
