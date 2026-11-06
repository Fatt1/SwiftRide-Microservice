using System.Net;
using System.Net.Http.Json;
using EventBus.Messages.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Payment.Application.Dtos;
using Payment.Application.Features.Payments.Commands.CreatePayment;
using Payment.Application.Features.Payments.Commands.RefundPayment;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.IntegrationTests.Common.Builders;
using Payment.IntegrationTests.Fixtures;
using Shared.Enums.Payments;
using Shouldly;
using Xunit;

namespace Payment.IntegrationTests.Endpoints;

public class PaymentEndpointsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task CreatePayment_WithWalletAndSufficientBalance_ReturnsOkAndDeductsBalanceAndPublishesEvent()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        using (var db = fixture.GetDbContext())
        {
            var riderWallet = new WalletBuilder().WithUserId(riderId).WithBalance(500_000m).Build();
            var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(0m).Build();
            await db.Wallets.AddRangeAsync(riderWallet, driverWallet);
            await db.SaveChangesAsync();
        }

        var tripId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        var command = new CreatePaymentCommand
        {
            TripId = tripId,
            RiderId = riderId,
            DriverId = driverId,
            Amount = 100_000m,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = idempotencyKey,
            Currency = "VND"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentDto>();
        result.ShouldNotBeNull();
        result.TripId.ShouldBe(tripId);
        result.Status.ShouldBe(PaymentStatus.Completed);
        result.Amount.ShouldBe(100_000m);

        using (var verifyDb = fixture.GetDbContext())
        {
            var persistedRiderWallet = await verifyDb.Wallets.SingleOrDefaultAsync(w => w.UserId == riderId);
            persistedRiderWallet.ShouldNotBeNull();
            persistedRiderWallet.Balance.ShouldBe(400_000m);

            var persistedDriverWallet = await verifyDb.Wallets.SingleOrDefaultAsync(w => w.UserId == driverId);
            persistedDriverWallet.ShouldNotBeNull();
            persistedDriverWallet.Balance.ShouldBe(100_000m);

            var ledgerEntries = await verifyDb.LedgerEntries.Where(l => l.PaymentId == result.PaymentId).ToListAsync();
            ledgerEntries.Count.ShouldBe(2);
            ledgerEntries.ShouldContain(l => l.EntryType == EntryType.Debit && l.Amount == 100_000m);
            ledgerEntries.ShouldContain(l => l.EntryType == EntryType.Credit && l.Amount == 100_000m);
        }

        await fixture.PublishEndpointMock.Received(1).Publish(
            Arg.Is<PaymentCompletedEvent>(e =>
                e.TripId == tripId &&
                e.PaymentId == result.PaymentId &&
                e.Amount == 100_000m &&
                e.PaymentMethod == PaymentMethod.Wallet),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePayment_WithWalletAndInsufficientBalance_ReturnsOkWithFailedStatusAndPublishesFailedEvent()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        using (var db = fixture.GetDbContext())
        {
            var riderWallet = new WalletBuilder().WithUserId(riderId).WithBalance(20_000m).Build();
            var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(0m).Build();
            await db.Wallets.AddRangeAsync(riderWallet, driverWallet);
            await db.SaveChangesAsync();
        }

        var tripId = Guid.NewGuid();
        var command = new CreatePaymentCommand
        {
            TripId = tripId,
            RiderId = riderId,
            DriverId = driverId,
            Amount = 100_000m,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = Guid.NewGuid(),
            Currency = "VND"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentDto>();
        result.ShouldNotBeNull();
        result.Status.ShouldBe(PaymentStatus.Failed);
        result.FailureReason.ShouldNotBeNull();
        result.FailureReason.ShouldContain("Insufficient balance");

        using (var verifyDb = fixture.GetDbContext())
        {
            var persistedRiderWallet = await verifyDb.Wallets.SingleOrDefaultAsync(w => w.UserId == riderId);
            persistedRiderWallet.ShouldNotBeNull();
            persistedRiderWallet.Balance.ShouldBe(20_000m);
        }

        await fixture.PublishEndpointMock.Received(1).Publish(
            Arg.Is<PaymentFailedEvent>(e =>
                e.TripId == tripId &&
                e.RiderId == riderId &&
                e.Amount == 100_000m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePayment_WithCardAndMockSuccessToken_ReturnsOkWithCompletedStatusAndPublishesEvent()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var tripId = Guid.NewGuid();
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var command = new CreatePaymentCommand
        {
            TripId = tripId,
            RiderId = riderId,
            DriverId = driverId,
            Amount = 80_000m,
            PaymentMethod = PaymentMethod.Card,
            GatewayToken = "mock-success",
            IdempotencyKey = Guid.NewGuid(),
            Currency = "VND"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentDto>();
        result.ShouldNotBeNull();
        result.Status.ShouldBe(PaymentStatus.Completed);
        result.PaymentMethod.ShouldBe(PaymentMethod.Card);

        await fixture.PublishEndpointMock.Received(1).Publish(
            Arg.Is<PaymentCompletedEvent>(e =>
                e.TripId == tripId &&
                e.PaymentId == result.PaymentId &&
                e.PaymentMethod == PaymentMethod.Card),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePayment_WithCardAndMockDeclineToken_ReturnsOkWithFailedStatus()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var command = new CreatePaymentCommand
        {
            TripId = Guid.NewGuid(),
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            Amount = 80_000m,
            PaymentMethod = PaymentMethod.Card,
            GatewayToken = "mock-decline",
            IdempotencyKey = Guid.NewGuid(),
            Currency = "VND"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentDto>();
        result.ShouldNotBeNull();
        result.Status.ShouldBe(PaymentStatus.Failed);
        result.FailureReason.ShouldBe("Mock card was declined.");
    }

    [Fact]
    public async Task CreatePayment_WhenCalledTwiceWithSameIdempotencyKey_ReturnsSamePaymentWithoutDuplicateCharge()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        using (var db = fixture.GetDbContext())
        {
            var riderWallet = new WalletBuilder().WithUserId(riderId).WithBalance(500_000m).Build();
            var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(0m).Build();
            await db.Wallets.AddRangeAsync(riderWallet, driverWallet);
            await db.SaveChangesAsync();
        }

        var tripId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        var command = new CreatePaymentCommand
        {
            TripId = tripId,
            RiderId = riderId,
            DriverId = driverId,
            Amount = 100_000m,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = idempotencyKey,
            Currency = "VND"
        };

        // Act - Call 1
        var response1 = await _client.PostAsJsonAsync("/api/v1/payments", command);
        response1.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result1 = await response1.Content.ReadFromJsonAsync<PaymentDto>();

        // Act - Call 2 with identical command
        var response2 = await _client.PostAsJsonAsync("/api/v1/payments", command);
        response2.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result2 = await response2.Content.ReadFromJsonAsync<PaymentDto>();

        // Assert
        result1.ShouldNotBeNull();
        result2.ShouldNotBeNull();
        result2.PaymentId.ShouldBe(result1.PaymentId);

        using (var verifyDb = fixture.GetDbContext())
        {
            var riderWallet = await verifyDb.Wallets.SingleAsync(w => w.UserId == riderId);
            riderWallet.Balance.ShouldBe(400_000m); // Deducted only once!
        }
    }

    [Fact]
    public async Task CreatePayment_WithConflictingRequestForSameTrip_ReturnsConflict()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var tripId = Guid.NewGuid();
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        using (var db = fixture.GetDbContext())
        {
            var payment = new PaymentTransactionBuilder()
                .WithTripId(tripId)
                .WithRiderId(riderId)
                .WithDriverId(driverId)
                .WithAmount(100_000m)
                .WithStatus(PaymentStatus.Completed)
                .Build();
            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();
        }

        // Different amount or different idempotency key for same trip
        var conflictingCommand = new CreatePaymentCommand
        {
            TripId = tripId,
            RiderId = riderId,
            DriverId = driverId,
            Amount = 200_000m,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = Guid.NewGuid(),
            Currency = "VND"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", conflictingCommand);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatePayment_WithInvalidCommand_ReturnsBadRequest()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var command = new CreatePaymentCommand
        {
            TripId = Guid.Empty, // Invalid empty Guid
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            Amount = -50_000m, // Invalid negative amount
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = Guid.NewGuid(),
            Currency = "VND"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPayments_WhenPaymentsExist_ReturnsOkWithPaymentList()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using (var db = fixture.GetDbContext())
        {
            var payment1 = new PaymentTransactionBuilder().WithAmount(50_000m).Build();
            var payment2 = new PaymentTransactionBuilder().WithAmount(70_000m).Build();
            await db.Payments.AddRangeAsync(payment1, payment2);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/v1/payments");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PaymentHistoryDto>>();
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetPaymentById_WhenExists_ReturnsOkWithPaymentDetails()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        Guid paymentId;
        using (var db = fixture.GetDbContext())
        {
            var payment = new PaymentTransactionBuilder().WithAmount(150_000m).Build();
            paymentId = payment.Id;
            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/{paymentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentDto>();
        result.ShouldNotBeNull();
        result.PaymentId.ShouldBe(paymentId);
        result.Amount.ShouldBe(150_000m);
    }

    [Fact]
    public async Task GetPaymentById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaymentByTripId_WhenExists_ReturnsOkWithPaymentHistory()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var tripId = Guid.NewGuid();
        using (var db = fixture.GetDbContext())
        {
            var payment = new PaymentTransactionBuilder()
                .WithTripId(tripId)
                .WithAmount(99_000m)
                .Build();
            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/trip/{tripId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentHistoryDto>();
        result.ShouldNotBeNull();
        result.TripId.ShouldBe(tripId);
        result.Amount.ShouldBe(99_000m);
    }

    [Fact]
    public async Task GetPaymentByTripId_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/trip/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RefundPayment_WhenCompletedTripPayment_ReturnsOkAndRestoresRiderBalance()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        Guid paymentId;

        using (var db = fixture.GetDbContext())
        {
            var riderWallet = new WalletBuilder().WithUserId(riderId).WithBalance(200_000m).Build();
            var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(100_000m).Build();
            await db.Wallets.AddRangeAsync(riderWallet, driverWallet);

            var payment = new PaymentTransactionBuilder()
                .WithRiderId(riderId)
                .WithDriverId(driverId)
                .WithAmount(100_000m)
                .WithPaymentMethod(PaymentMethod.Wallet)
                .WithStatus(PaymentStatus.Completed)
                .Build();
            paymentId = payment.Id;

            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();
        }

        var command = new RefundPaymentCommand { Reason = "Trip was cancelled" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/payments/{paymentId}/refund", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RefundDto>();
        result.ShouldNotBeNull();
        result.PaymentId.ShouldBe(paymentId);
        result.Amount.ShouldBe(100_000m);
        result.Status.ShouldBe(RefundStatus.Completed);

        using (var verifyDb = fixture.GetDbContext())
        {
            var payment = await verifyDb.Payments.FindAsync(paymentId);
            payment.ShouldNotBeNull();
            payment.Status.ShouldBe(PaymentStatus.Refunded);

            var riderWallet = await verifyDb.Wallets.SingleAsync(w => w.UserId == riderId);
            riderWallet.Balance.ShouldBe(300_000m); // 200k + 100k refunded

            var driverWallet = await verifyDb.Wallets.SingleAsync(w => w.UserId == driverId);
            driverWallet.Balance.ShouldBe(0m); // 100k - 100k deducted

            var refund = await verifyDb.Refunds.SingleOrDefaultAsync(r => r.PaymentId == paymentId);
            refund.ShouldNotBeNull();
            refund.Status.ShouldBe(RefundStatus.Completed);
        }
    }

    [Fact]
    public async Task RefundPayment_WhenCalledTwice_ReturnsExistingRefundIdempotently()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        Guid paymentId;

        using (var db = fixture.GetDbContext())
        {
            var riderWallet = new WalletBuilder().WithUserId(riderId).WithBalance(200_000m).Build();
            var driverWallet = new WalletBuilder().WithUserId(driverId).WithBalance(100_000m).Build();
            await db.Wallets.AddRangeAsync(riderWallet, driverWallet);

            var payment = new PaymentTransactionBuilder()
                .WithRiderId(riderId)
                .WithDriverId(driverId)
                .WithAmount(100_000m)
                .WithPaymentMethod(PaymentMethod.Wallet)
                .WithStatus(PaymentStatus.Completed)
                .Build();
            paymentId = payment.Id;

            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();
        }

        var command = new RefundPaymentCommand { Reason = "Driver cancelled" };

        // Act - 1st call
        var response1 = await _client.PostAsJsonAsync($"/api/v1/payments/{paymentId}/refund", command);
        response1.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result1 = await response1.Content.ReadFromJsonAsync<RefundDto>();

        // Act - 2nd call
        var response2 = await _client.PostAsJsonAsync($"/api/v1/payments/{paymentId}/refund", command);
        response2.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result2 = await response2.Content.ReadFromJsonAsync<RefundDto>();

        // Assert
        result1.ShouldNotBeNull();
        result2.ShouldNotBeNull();
        result2.RefundId.ShouldBe(result1.RefundId);

        using (var verifyDb = fixture.GetDbContext())
        {
            var riderWallet = await verifyDb.Wallets.SingleAsync(w => w.UserId == riderId);
            riderWallet.Balance.ShouldBe(300_000m); // Balance not refunded twice!
        }
    }

    [Fact]
    public async Task RefundPayment_WhenPaymentNotFound_ReturnsNotFound()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        var nonExistentId = Guid.NewGuid();
        var command = new RefundPaymentCommand { Reason = "No payment" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/payments/{nonExistentId}/refund", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RefundPayment_WhenPaymentNotCompleted_ReturnsConflict()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        Guid paymentId;
        using (var db = fixture.GetDbContext())
        {
            var payment = new PaymentTransactionBuilder()
                .WithStatus(PaymentStatus.Pending)
                .Build();
            paymentId = payment.Id;
            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();
        }

        var command = new RefundPaymentCommand { Reason = "Not yet paid" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/payments/{paymentId}/refund", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetLedger_ReturnsAllLedgerEntries()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using (var db = fixture.GetDbContext())
        {
            var wallet = new WalletBuilder().Build();
            await db.Wallets.AddAsync(wallet);

            var payment = new PaymentTransactionBuilder().Build();
            await db.Payments.AddAsync(payment);

            var entry1 = LedgerEntry.Create(payment.Id, wallet.Id, PaymentMethod.Wallet, EntryType.Debit, 50_000m, "Debit entry");
            var entry2 = LedgerEntry.Create(payment.Id, wallet.Id, PaymentMethod.Wallet, EntryType.Credit, 50_000m, "Credit entry");
            await db.LedgerEntries.AddRangeAsync(entry1, entry2);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/v1/payments/ledger");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<LedgerEntryDto>>();
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetLedgerByPaymentId_WhenPaymentExists_ReturnsLedgerEntries()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        Guid paymentId;
        using (var db = fixture.GetDbContext())
        {
            var wallet = new WalletBuilder().Build();
            await db.Wallets.AddAsync(wallet);

            var payment = new PaymentTransactionBuilder().Build();
            paymentId = payment.Id;
            await db.Payments.AddAsync(payment);

            var entry = LedgerEntry.Create(payment.Id, wallet.Id, PaymentMethod.Wallet, EntryType.Debit, 60_000m, "Test");
            await db.LedgerEntries.AddAsync(entry);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/ledger/payment/{paymentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<LedgerEntryDto>>();
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].PaymentId.ShouldBe(paymentId);
        result[0].Amount.ShouldBe(60_000m);
    }

    [Fact]
    public async Task GetLedgerByPaymentId_WhenPaymentDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/ledger/payment/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
