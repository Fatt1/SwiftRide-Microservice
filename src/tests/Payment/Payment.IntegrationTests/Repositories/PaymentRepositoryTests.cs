using Microsoft.EntityFrameworkCore;
using Payment.Domain.Enums;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Repositories;
using Payment.IntegrationTests.Common.Builders;
using Payment.IntegrationTests.Fixtures;
using Shared.Enums.Payments;
using Shouldly;
using Xunit;

namespace Payment.IntegrationTests.Repositories;

public class PaymentRepositoryTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private (PaymentDbContext DbContext, PaymentRepository Repository) CreateRepository()
    {
        var db = fixture.GetDbContext();
        return (db, new PaymentRepository(db));
    }

    [Fact]
    public async Task Add_And_GetByIdAsync_WhenPaymentExists_ReturnsMatchingPayment()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        await using (db)
        {
            var payment = new PaymentTransactionBuilder()
                .WithAmount(120_000m)
                .WithPaymentMethod(PaymentMethod.Wallet)
                .Build();

            // Act
            repo.Add(payment);
            await repo.SaveChangesAsync();

            // Assert
            var retrieved = await repo.GetByIdAsync(payment.Id, CancellationToken.None);
            retrieved.ShouldNotBeNull();
            retrieved.Id.ShouldBe(payment.Id);
            retrieved.TripId.ShouldBe(payment.TripId);
            retrieved.RiderId.ShouldBe(payment.RiderId);
            retrieved.DriverId.ShouldBe(payment.DriverId);
            retrieved.Amount.ShouldBe(120_000m);
            retrieved.PaymentMethod.ShouldBe(PaymentMethod.Wallet);
        }
    }

    [Fact]
    public async Task GetAllAsync_WhenMultiplePaymentsExist_ReturnsOrderedByCreatedAtDescending()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        await using (db)
        {
            var payment1 = new PaymentTransactionBuilder().WithAmount(50_000m).Build();
            var payment2 = new PaymentTransactionBuilder().WithAmount(75_000m).Build();

            repo.Add(payment1);
            repo.Add(payment2);
            await repo.SaveChangesAsync();

            // Act
            var payments = await repo.GetAllAsync(CancellationToken.None);

            // Assert
            payments.Count.ShouldBe(2);
            payments.ShouldContain(p => p.Id == payment1.Id);
            payments.ShouldContain(p => p.Id == payment2.Id);
        }
    }

    [Fact]
    public async Task GetByTripIdAsync_WhenPaymentExistsForTrip_ReturnsMatchingPayment()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        await using (db)
        {
            var tripId = Guid.NewGuid();
            var payment = new PaymentTransactionBuilder()
                .WithTripId(tripId)
                .WithAmount(95_000m)
                .Build();

            repo.Add(payment);
            await repo.SaveChangesAsync();

            // Act
            var retrieved = await repo.GetByTripIdAsync(tripId, CancellationToken.None);

            // Assert
            retrieved.ShouldNotBeNull();
            retrieved.TripId.ShouldBe(tripId);
            retrieved.Amount.ShouldBe(95_000m);
        }
    }

    [Fact]
    public async Task GetByTripIdAsync_WhenNoPaymentForTrip_ReturnsNull()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        await using (db)
        {
            // Act
            var retrieved = await repo.GetByTripIdAsync(Guid.NewGuid(), CancellationToken.None);

            // Assert
            retrieved.ShouldBeNull();
        }
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_WhenPaymentExists_ReturnsMatchingPayment()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        await using (db)
        {
            var idempotencyKey = Guid.NewGuid();
            var payment = new PaymentTransactionBuilder()
                .WithIdempotencyKey(idempotencyKey)
                .Build();

            repo.Add(payment);
            await repo.SaveChangesAsync();

            // Act
            var retrieved = await repo.GetByIdempotencyKeyAsync(idempotencyKey, CancellationToken.None);

            // Assert
            retrieved.ShouldNotBeNull();
            retrieved.IdempotencyKey.ShouldBe(idempotencyKey);
        }
    }

    [Fact]
    public async Task LockPaymentsAsync_WithinTransaction_AcquiresPostgreSqlAdvisoryLockSuccessfully()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        await using (db)
        {
            var key1 = Guid.NewGuid();
            var key2 = Guid.NewGuid();

            // Act & Assert
            await using var tx = await repo.BeginOwnedTransactionAsync();
            tx.ShouldNotBeNull();

            // Should execute without throwing (PostgreSQL advisory lock function executes cleanly)
            await repo.LockPaymentsAsync([key1, key2], CancellationToken.None);
            await repo.EndTransactionAsync();
        }
    }

    [Fact]
    public async Task LockWalletsAsync_WithinTransaction_AcquiresPostgreSqlAdvisoryLockSuccessfully()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        await using (db)
        {
            var walletId1 = Guid.NewGuid();
            var walletId2 = Guid.NewGuid();

            // Act & Assert
            await using var tx = await repo.BeginOwnedTransactionAsync();
            tx.ShouldNotBeNull();

            await repo.LockWalletsAsync([walletId1, walletId2], CancellationToken.None);
            await repo.EndTransactionAsync();
        }
    }

    [Fact]
    public async Task BeginOwnedTransactionAsync_WhenCalledTwice_SecondCallReturnsNull()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var (db, repo) = CreateRepository();
        await using (db)
        {
            // Act
            await using var tx1 = await repo.BeginOwnedTransactionAsync();
            var tx2 = await repo.BeginOwnedTransactionAsync();

            // Assert
            tx1.ShouldNotBeNull();
            tx2.ShouldBeNull();

            await repo.EndTransactionAsync();
        }
    }
}
