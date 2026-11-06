using EventBus.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Payment.Application.Abstractions;
using Payment.Application.Features.Payments.Commands.CreatePayment;
using Payment.Application.Payments.Contexts;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Repositories;
using Payment.UnitTests.Common.Builders;
using Shared;
using Shared.Enums.Payments;
using Shouldly;
using Xunit;

namespace Payment.UnitTests.Features.Payments.Commands;

public class CreatePaymentCommandHandlerTests
{
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IPaymentTransaction _transactions = Substitute.For<IPaymentTransaction>();
    private readonly IPaymentStrategyFactory _strategies = Substitute.For<IPaymentStrategyFactory>();
    private readonly IPaymentStrategy _paymentStrategy = Substitute.For<IPaymentStrategy>();
    private readonly IPublishEndpoint _publisher = Substitute.For<IPublishEndpoint>();
    private readonly ILogger<CreatePaymentCommandHandler> _logger = Substitute.For<ILogger<CreatePaymentCommandHandler>>();
    private readonly CreatePaymentCommandHandler _handler;

    public CreatePaymentCommandHandlerTests()
    {
        _strategies.GetStrategy(Arg.Any<PaymentMethod>()).Returns(_paymentStrategy);
        _transactions.BeginOwnedTransactionAsync().Returns(Substitute.For<IAsyncDisposable>());

        _handler = new CreatePaymentCommandHandler(
            _payments,
            _transactions,
            _strategies,
            _publisher,
            _logger);
    }

    [Fact]
    public async Task Handle_WhenIdempotentRequestMatchesExisting_ReturnsExistingPaymentWithoutCharging()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        var riderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        const decimal amount = 100_000m;

        var existing = new PaymentTransactionBuilder()
            .WithTripId(tripId)
            .WithRiderId(riderId)
            .WithDriverId(driverId)
            .WithAmount(amount)
            .WithIdempotencyKey(idempotencyKey)
            .WithPaymentMethod(PaymentMethod.Wallet)
            .WithStatus(PaymentStatus.Completed)
            .Build();

        _payments.GetByTripIdAsync(tripId, Arg.Any<CancellationToken>()).Returns(existing);

        var command = new CreatePaymentCommand
        {
            TripId = tripId,
            RiderId = riderId,
            DriverId = driverId,
            Amount = amount,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = idempotencyKey,
            Currency = "VND"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.PaymentId.ShouldBe(existing.Id);
        result.Value.Status.ShouldBe(PaymentStatus.Completed);

        // Verify strategy was never called to re-charge
        await _paymentStrategy.DidNotReceive().ChargeAsync(Arg.Any<PaymentExecutionContext>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<PaymentCompletedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenExistingPaymentConflicts_ReturnsConflictError()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();

        // Existing payment has different amount
        var existing = new PaymentTransactionBuilder()
            .WithTripId(tripId)
            .WithAmount(200_000m)
            .WithIdempotencyKey(idempotencyKey)
            .Build();

        _payments.GetByTripIdAsync(tripId, Arg.Any<CancellationToken>()).Returns(existing);

        var command = new CreatePaymentCommand
        {
            TripId = tripId,
            RiderId = existing.RiderId,
            DriverId = existing.DriverId!.Value,
            Amount = 100_000m, // different amount
            PaymentMethod = existing.PaymentMethod,
            IdempotencyKey = idempotencyKey
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ConflictError>();
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceeds_MarksCompletedPublishesCompletedEventAndCommits()
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            TripId = Guid.NewGuid(),
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            Amount = 150_000m,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = Guid.NewGuid(),
            Currency = "VND"
        };

        _payments.GetByTripIdAsync(command.TripId, Arg.Any<CancellationToken>()).Returns((PaymentTransaction?)null);
        _payments.GetByIdempotencyKeyAsync(command.IdempotencyKey, Arg.Any<CancellationToken>()).Returns((PaymentTransaction?)null);

        _paymentStrategy.ChargeAsync(Arg.Any<PaymentExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(PaymentStatus.Completed);

        _payments.Received(1).Add(Arg.Is<PaymentTransaction>(p => p.Status == PaymentStatus.Completed));
        await _publisher.Received(1).Publish(Arg.Is<PaymentCompletedEvent>(e =>
            e.TripId == command.TripId &&
            e.Amount == command.Amount &&
            e.RiderId == command.RiderId), Arg.Any<CancellationToken>());
        await _transactions.Received(1).EndTransactionAsync();
    }

    [Fact]
    public async Task Handle_WhenPaymentStrategyFails_MarksFailedPublishesFailedEventAndSaves()
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            TripId = Guid.NewGuid(),
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            Amount = 150_000m,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = Guid.NewGuid(),
            Currency = "VND"
        };

        _payments.GetByTripIdAsync(command.TripId, Arg.Any<CancellationToken>()).Returns((PaymentTransaction?)null);
        _payments.GetByIdempotencyKeyAsync(command.IdempotencyKey, Arg.Any<CancellationToken>()).Returns((PaymentTransaction?)null);

        const string failureMessage = "Insufficient balance in rider wallet";
        _paymentStrategy.ChargeAsync(Arg.Any<PaymentExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new ConflictError(failureMessage)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue(); // Handler still returns payment dto with Failed status
        result.Value.Status.ShouldBe(PaymentStatus.Failed);
        result.Value.FailureReason.ShouldBe(failureMessage);

        _payments.Received(1).Add(Arg.Is<PaymentTransaction>(p => p.Status == PaymentStatus.Failed));
        await _publisher.Received(1).Publish(Arg.Is<PaymentFailedEvent>(e =>
            e.TripId == command.TripId &&
            e.Reason == failureMessage), Arg.Any<CancellationToken>());
        await _transactions.Received(1).EndTransactionAsync();
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_RollsBackTransactionAndThrows()
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            TripId = Guid.NewGuid(),
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            Amount = 150_000m,
            PaymentMethod = PaymentMethod.Wallet,
            IdempotencyKey = Guid.NewGuid()
        };

        _payments.GetByTripIdAsync(command.TripId, Arg.Any<CancellationToken>())
            .Returns<PaymentTransaction?>(_ => throw new InvalidOperationException("DB connection error"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));

        await _transactions.Received(1).RollBackTransactionAsync();
    }
}
