using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Repositories;
using Payment.IntegrationTests.Common.Builders;
using Payment.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

namespace Payment.IntegrationTests.Repositories;

public class WalletRepositoryTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private (PaymentDbContext DbContext, WalletRepository Repository) CreateRepository()
    {
        var db = fixture.GetDbContext();
        return (db, new WalletRepository(db));
    }

    [Fact]
    public async Task Add_And_GetByUserIdAsync_WhenWalletExists_ReturnsMatchingWallet()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        await using (db)
        {
            var userId = Guid.NewGuid();
            var wallet = new WalletBuilder()
                .WithUserId(userId)
                .WithBalance(300_000m)
                .WithCurrency("VND")
                .Build();

            // Act
            repo.Add(wallet);
            await repo.SaveChangesAsync();

            // Assert
            var retrieved = await repo.GetByUserIdAsync(userId, CancellationToken.None);
            retrieved.ShouldNotBeNull();
            retrieved.UserId.ShouldBe(userId);
            retrieved.Balance.ShouldBe(300_000m);
            retrieved.Currency.ShouldBe("VND");
        }
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenWalletDoesNotExist_ReturnsNull()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        await using (db)
        {
            // Act
            var retrieved = await repo.GetByUserIdAsync(Guid.NewGuid(), CancellationToken.None);

            // Assert
            retrieved.ShouldBeNull();
        }
    }

    [Fact]
    public async Task GetAllAsync_WhenMultipleWalletsExist_ReturnsAllWallets()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        await using (db)
        {
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();
            var wallet1 = new WalletBuilder().WithUserId(user1).WithBalance(100_000m).Build();
            var wallet2 = new WalletBuilder().WithUserId(user2).WithBalance(200_000m).Build();

            repo.Add(wallet1);
            repo.Add(wallet2);
            await repo.SaveChangesAsync();

            // Act
            var wallets = await repo.GetAllAsync(CancellationToken.None);

            // Assert
            wallets.Count.ShouldBe(2);
            wallets.ShouldContain(w => w.UserId == user1);
            wallets.ShouldContain(w => w.UserId == user2);
        }
    }
}
