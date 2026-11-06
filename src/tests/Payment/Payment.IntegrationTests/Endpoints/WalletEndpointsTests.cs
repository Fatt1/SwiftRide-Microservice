using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Payment.API.Endpoints;
using Payment.Application.Dtos;
using Payment.Domain.Enums;
using Payment.IntegrationTests.Common.Builders;
using Payment.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

namespace Payment.IntegrationTests.Endpoints;

public class WalletEndpointsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task TopUpWallet_WhenWalletExists_ReturnsOkAndIncreasesBalanceAndCreatesLedger()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var userId = Guid.NewGuid();

        using (var db = fixture.GetDbContext())
        {
            var wallet = new WalletBuilder()
                .WithUserId(userId)
                .WithBalance(100_000m)
                .Build();
            await db.Wallets.AddAsync(wallet);
            await db.SaveChangesAsync();
        }

        var idempotencyKey = Guid.NewGuid();
        var request = new WalletEndpoints.TopUpRequest(idempotencyKey, 200_000m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/payments/wallets/{userId}/top-up", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WalletDto>();
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(userId);
        result.Balance.ShouldBe(300_000m);

        using (var verifyDb = fixture.GetDbContext())
        {
            var wallet = await verifyDb.Wallets.SingleAsync(w => w.UserId == userId);
            wallet.Balance.ShouldBe(300_000m);

            var ledger = await verifyDb.LedgerEntries.Where(l => l.WalletId == wallet.Id).ToListAsync();
            ledger.Count.ShouldBe(1);
            ledger[0].EntryType.ShouldBe(EntryType.Credit);
            ledger[0].Amount.ShouldBe(200_000m);
            ledger[0].Description.ShouldBe("Wallet top-up");
        }
    }

    [Fact]
    public async Task TopUpWallet_WhenCalledTwiceWithSameIdempotencyKey_ReturnsSameBalanceWithoutDuplicateCredit()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var userId = Guid.NewGuid();

        using (var db = fixture.GetDbContext())
        {
            var wallet = new WalletBuilder()
                .WithUserId(userId)
                .WithBalance(100_000m)
                .Build();
            await db.Wallets.AddAsync(wallet);
            await db.SaveChangesAsync();
        }

        var idempotencyKey = Guid.NewGuid();
        var request = new WalletEndpoints.TopUpRequest(idempotencyKey, 150_000m);

        // Act - 1st call
        var response1 = await _client.PostAsJsonAsync($"/api/v1/payments/wallets/{userId}/top-up", request);
        response1.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result1 = await response1.Content.ReadFromJsonAsync<WalletDto>();

        // Act - 2nd call with same idempotency key
        var response2 = await _client.PostAsJsonAsync($"/api/v1/payments/wallets/{userId}/top-up", request);
        response2.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result2 = await response2.Content.ReadFromJsonAsync<WalletDto>();

        // Assert
        result1.ShouldNotBeNull();
        result2.ShouldNotBeNull();
        result1.Balance.ShouldBe(250_000m);
        result2.Balance.ShouldBe(250_000m);

        using (var verifyDb = fixture.GetDbContext())
        {
            var wallet = await verifyDb.Wallets.SingleAsync(w => w.UserId == userId);
            wallet.Balance.ShouldBe(250_000m); // Credited only once!
        }
    }

    [Fact]
    public async Task TopUpWallet_WhenWalletNotFound_ReturnsNotFound()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var nonExistentUserId = Guid.NewGuid();
        var request = new WalletEndpoints.TopUpRequest(Guid.NewGuid(), 100_000m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/payments/wallets/{nonExistentUserId}/top-up", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TopUpWallet_WithInvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var request = new WalletEndpoints.TopUpRequest(Guid.NewGuid(), -50_000m);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/payments/wallets/{userId}/top-up", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWalletByUserId_WhenWalletExists_ReturnsExistingWallet()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var userId = Guid.NewGuid();

        using (var db = fixture.GetDbContext())
        {
            var wallet = new WalletBuilder()
                .WithUserId(userId)
                .WithBalance(450_000m)
                .Build();
            await db.Wallets.AddAsync(wallet);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/wallets/user/{userId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WalletDto>();
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(userId);
        result.Balance.ShouldBe(450_000m);
    }

    [Fact]
    public async Task GetWalletByUserId_WhenWalletDoesNotExist_CreatesNewWalletAndReturnsIt()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var newUserId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/wallets/user/{newUserId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WalletDto>();
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(newUserId);
        result.Balance.ShouldBe(0m);
        result.Currency.ShouldBe("VND");

        using (var verifyDb = fixture.GetDbContext())
        {
            var created = await verifyDb.Wallets.SingleOrDefaultAsync(w => w.UserId == newUserId);
            created.ShouldNotBeNull();
            created.Balance.ShouldBe(0m);
        }
    }

    [Fact]
    public async Task GetLedgerByUserId_WhenLedgerEntriesExist_ReturnsUserLedger()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var userId = Guid.NewGuid();

        using (var db = fixture.GetDbContext())
        {
            var wallet = new WalletBuilder().WithUserId(userId).WithBalance(50_000m).Build();
            await db.Wallets.AddAsync(wallet);
            await db.SaveChangesAsync();
        }

        // Top up to generate a ledger entry
        var topUpRequest = new WalletEndpoints.TopUpRequest(Guid.NewGuid(), 200_000m);
        await _client.PostAsJsonAsync($"/api/v1/payments/wallets/{userId}/top-up", topUpRequest);

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/wallets/user/{userId}/ledger");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<LedgerEntryDto>>();
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].EntryType.ShouldBe(EntryType.Credit);
        result[0].Amount.ShouldBe(200_000m);
    }

    [Fact]
    public async Task GetAllWallets_ReturnsAllExistingWallets()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using (var db = fixture.GetDbContext())
        {
            var w1 = new WalletBuilder().WithBalance(100_000m).Build();
            var w2 = new WalletBuilder().WithBalance(200_000m).Build();
            await db.Wallets.AddRangeAsync(w1, w2);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/v1/payments/wallets");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<WalletDto>>();
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
    }
}
