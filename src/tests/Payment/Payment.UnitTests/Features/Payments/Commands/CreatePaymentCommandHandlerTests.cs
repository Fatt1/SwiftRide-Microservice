using EventBus.Messages.Events;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Payment.Application.Abstractions;
using Payment.Application.Features.Payments.Commands.CreatePayment;
using Payment.Application.Payments;
using Payment.Application.Payments.Contexts;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Repositories;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Payments;
using Payment.Infrastructure.Strategies;
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
    private readonly IAsyncDisposable _ownedTransaction = Substitute.For<IAsyncDisposable>();
    private readonly IPaymentStrategyFactory _strategies = Substitute.For<IPaymentStrategyFactory>();
    private readonly IPaymentStrategy _paymentStrategy = Substitute.For<IPaymentStrategy>();
    private readonly IPublishEndpoint _publisher = Substitute.For<IPublishEndpoint>();
    private readonly ILogger<CreatePaymentCommandHandler> _logger = Substitute.For<ILogger<CreatePaymentCommandHandler>>();
    private readonly CreatePaymentCommandHandler _handler;

    public CreatePaymentCommandHandlerTests()
    {
        _strategies.GetStrategy(Arg.Any<PaymentMethod>()).Returns(_paymentStrategy);
        _transactions.BeginOwnedTransactionAsync().Returns(_ownedTransaction);

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

        _payments.GetByIdempotencyKeyAsync(idempotencyKey, Arg.Any<CancellationToken>()).Returns(existing);

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

        _payments.GetByIdempotencyKeyAsync(idempotencyKey, Arg.Any<CancellationToken>()).Returns(existing);

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
    public async Task Handle_WhenDatabaseOperationThrows_DisposesOwnedTransactionAndPropagatesError()
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

        _payments.GetByIdempotencyKeyAsync(command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns<PaymentTransaction?>(_ => throw new InvalidOperationException("DB connection error"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));

        await _ownedTransaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Handle_WhenGatewayErrorIsTransient_PropagatesErrorWithoutSavingFailure()
    {
        var command = new CreatePaymentCommand
        {
            TripId = Guid.NewGuid(),
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            Amount = 150_000m,
            PaymentMethod = PaymentMethod.Card,
            GatewayToken = "mock-retry-exhausted",
            IdempotencyKey = Guid.NewGuid()
        };
        var gatewayError = new TransientCardPaymentException("Gateway timed out.");
        _paymentStrategy.ChargeAsync(Arg.Any<PaymentExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Result>(gatewayError));

        var exception = await Should.ThrowAsync<TransientCardPaymentException>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.ShouldBeSameAs(gatewayError);
        _payments.DidNotReceive().Add(Arg.Any<PaymentTransaction>());
        await _publisher.DidNotReceive().Publish(Arg.Any<PaymentFailedEvent>(), Arg.Any<CancellationToken>());
        await _transactions.DidNotReceive().EndTransactionAsync();
        await _ownedTransaction.Received(1).DisposeAsync();
    }
    [Fact]
    public async Task Handle_AlreadySettledTrip_RespondsForCurrentAttemptWithoutChargingAgain()
    {
        // Arrange
        var existing = new PaymentTransactionBuilder().WithStatus(PaymentStatus.Completed).Build();
        var key = Guid.NewGuid();
        _payments.GetCompletedByTripIdAsync(existing.TripId!.Value, Arg.Any<CancellationToken>()).Returns(existing);
        var command = new CreatePaymentCommand { TripId = existing.TripId.Value, RiderId = existing.RiderId,
            DriverId = existing.DriverId!.Value, Amount = existing.Amount, Currency = existing.Currency,
            PaymentMethod = existing.PaymentMethod, IdempotencyKey = key };
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        // Assert
        Assert.True(result.IsSuccess); Assert.Equal(existing.Id, result.Value.PaymentId);
        await _paymentStrategy.DidNotReceive().ChargeAsync(Arg.Any<PaymentExecutionContext>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<PaymentCompletedEvent>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_SameKeyWithDifferentDriver_RejectsWithoutCharging()
    {
        // Arrange
        var existing = new PaymentTransactionBuilder().WithStatus(PaymentStatus.Completed).Build();
        _payments.GetByIdempotencyKeyAsync(existing.IdempotencyKey, Arg.Any<CancellationToken>()).Returns(existing);
        var command = new CreatePaymentCommand { TripId = existing.TripId!.Value, RiderId = existing.RiderId,
            DriverId = Guid.NewGuid(), Amount = existing.Amount, Currency = existing.Currency,
            PaymentMethod = existing.PaymentMethod, IdempotencyKey = existing.IdempotencyKey };
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        // Assert
        Assert.IsType<ConflictError>(result.Error);
        await _paymentStrategy.DidNotReceive().ChargeAsync(Arg.Any<PaymentExecutionContext>(), Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task Consume_DifferentEventIdsWithSameIdempotencyKey_ReturnsExistingPaymentAndLogsWarnings()
    {
        // Arrange
        var existing = new PaymentTransactionBuilder().WithStatus(PaymentStatus.Completed).Build();
        _payments.GetByIdempotencyKeyAsync(existing.IdempotencyKey, Arg.Any<CancellationToken>()).Returns(existing);
        var sender = Substitute.For<MediatR.ISender>();
        sender.Send(Arg.Any<CreatePaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => _handler.Handle(call.ArgAt<CreatePaymentCommand>(0), call.ArgAt<CancellationToken>(1)));
        var consumer = new Payment.Infrastructure.Messaging.TripDropOffConsumer(sender,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Payment.Infrastructure.Messaging.TripDropOffConsumer>.Instance);

        // Act: event identity changes, payment attempt identity stays the same.
        for (var i = 0; i < 2; i++)
        {
            var context = Substitute.For<ConsumeContext<TripDropOffEvent>>();
            context.Message.Returns(new TripDropOffEvent
            {
                Id = Guid.NewGuid(), IdempotencyKey = existing.IdempotencyKey,
                TripId = existing.TripId!.Value, RiderId = existing.RiderId, DriverId = existing.DriverId!.Value,
                Amount = existing.Amount, Currency = existing.Currency, PaymentMethod = existing.PaymentMethod
            });
            await consumer.Consume(context);
        }

        // Assert
        await _payments.Received(2).GetByIdempotencyKeyAsync(existing.IdempotencyKey, Arg.Any<CancellationToken>());
        await _paymentStrategy.DidNotReceive().ChargeAsync(Arg.Any<PaymentExecutionContext>(), Arg.Any<CancellationToken>());
        Assert.Equal(2, _logger.ReceivedCalls().Count(call =>
            call.GetMethodInfo().Name == "Log" && Equals(call.GetArguments()[0], LogLevel.Warning) &&
            call.GetArguments()[2]!.ToString()!.Contains("IdempotencyKey=")));
    }

    [Theory]
    [InlineData("mock-retry-success", 3, false)]
    [InlineData("mock-retry-exhausted", 4, true)]
    public async Task Consume_WhenGatewayThrowsTransientError_RetriesWithSamePaymentKey(
        string token, int expectedAttempts, bool expectFault)
    {
        var message = new TripDropOffEvent
        {
            TripId = Guid.NewGuid(),
            RiderId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            IdempotencyKey = Guid.NewGuid(),
            Amount = 150_000m,
            PaymentMethod = PaymentMethod.Card,
            GatewayToken = token
        };
        var wallets = Substitute.For<IWalletRepository>();
        var ledger = Substitute.For<ILedgerRepository>();
        var driver = new WalletBuilder().WithUserId(message.DriverId).WithBalance(0m).Build();
        wallets.GetByUserIdAsync(message.DriverId, Arg.Any<CancellationToken>()).Returns(driver);
        _strategies.GetStrategy(PaymentMethod.Card)
            .Returns(new CardPaymentStrategy(wallets, new MockCardPaymentProcessor(), new LedgerPostingService(ledger)));
        var paymentHandled = new TaskCompletionSource<PaymentStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<CreatePaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var result = await _handler.Handle(call.ArgAt<CreatePaymentCommand>(0), call.ArgAt<CancellationToken>(1));
                paymentHandled.TrySetResult(result.Value.Status);
                return result;
            });

        await using var services = new ServiceCollection()
            .AddSingleton(sender)
            .AddMassTransitTestHarness(bus =>
            {
                bus.AddConsumer<TripDropOffConsumer>();
                bus.UsingInMemory((context, transport) =>
                {
                    transport.UseMessageRetry(retry => retry.Immediate(3));
                    transport.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);
        var harness = services.GetRequiredService<ITestHarness>();
        await harness.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await harness.Bus.Publish(message, timeout.Token);
        if (expectFault)
            (await harness.Published.Any<Fault<TripDropOffEvent>>(timeout.Token)).ShouldBeTrue();
        else
            (await paymentHandled.Task.WaitAsync(timeout.Token)).ShouldBe(PaymentStatus.Completed);

        var commands = sender.ReceivedCalls().Select(call => call.GetArguments()[0]).OfType<CreatePaymentCommand>().ToArray();
        commands.Select(command => command.IdempotencyKey)
            .ShouldBe(Enumerable.Repeat(message.IdempotencyKey, expectedAttempts));
        commands.Select(command => command.RetryAttempt).ShouldBe(Enumerable.Range(0, expectedAttempts));
        await _publisher.Received(expectFault ? 0 : 1)
            .Publish(Arg.Any<PaymentCompletedEvent>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<PaymentFailedEvent>(), Arg.Any<CancellationToken>());
        ledger.Received(expectFault ? 0 : 2).Add(Arg.Any<LedgerEntry>());
        driver.Balance.ShouldBe(expectFault ? 0m : message.Amount);
    }
}
