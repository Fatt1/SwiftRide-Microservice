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
        logger.LogInformation("Nhận UserCreated event: MessageId={MessageId}, UserId={UserId}, RetryAttempt={RetryAttempt}",
            context.MessageId, context.Message.UserId, context.GetRetryAttempt());
        if (!Guid.TryParse(context.Message.UserId, out var userId) || userId == Guid.Empty)
        {
            logger.LogWarning("Bỏ xử lý UserCreated event: MessageId={MessageId}, UserId={UserId} không phải GUID hợp lệ; message sẽ vào queue lỗi",
                context.MessageId, context.Message.UserId);
            throw new ArgumentException($"Invalid UserId in UserCreated event: {context.Message.UserId}");
        }

        if (await wallets.GetByUserIdAsync(userId, context.CancellationToken) is not null)
        {
            logger.LogInformation("Bỏ qua UserCreated event gửi lại: MessageId={MessageId}, UserId={UserId} đã có ví; không tạo ví thứ hai",
                context.MessageId, userId);
            return;
        }

        wallets.Add(Wallet.Create(userId));
        await dbContext.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Đã lưu ví mới từ UserCreated event: MessageId={MessageId}, UserId={UserId}, Balance=0 VND; đang chờ transaction của consumer hoàn tất",
            context.MessageId, userId);
    }
}
