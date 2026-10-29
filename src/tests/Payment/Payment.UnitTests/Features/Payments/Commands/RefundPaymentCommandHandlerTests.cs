using Microsoft.Extensions.Logging;
using NSubstitute;
using Payment.Application.Abstractions;
using Payment.Application.Features.Payments.Commands.RefundPayment;
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

public class RefundPaymentCommandHandlerTests
{
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IPaymentTransaction _transactions = Substitute.For<IPaymentTransaction>();
    private readonly IRefundRepository _refunds = Substitute.For<IRefundRepository>();
    private readonly IPaymentStrategyFactory _strategies = Substitute.For<IPaymentStrategyFactory>();
    private readonly IPaymentStrategy _paymentStrategy = Substitute.For<IPaymentStrategy>();
    private readonly ILogger<RefundPaymentCommandHandler> _logger = Substitute.For<ILogger<RefundPaymentCommandHandler>>();
    private readonly RefundPaymentCommandHandler _handler;

    public RefundPaymentCommandHandlerTests()
    {
        _strategies.GetStrategy(Arg.Any<PaymentMethod>()).Returns(_paymentStrategy);
        _transactions.BeginOwnedTransactionAsync().Returns(Substitute.For<IAsyncDisposable>());

        _handler = new RefundPaymentCommandHandler(
            _payments,
            _transactions,
            _refunds,
            _strategies,
            _logger);
    }

    [Fact]
    public async Task Handle_WhenPaymentNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        _payments.GetByIdAsync(paymentId, Arg.Any<CancellationToken>()).Returns((PaymentTransaction?)null);

        var command = new RefundPaymentCommand
        {
            PaymentId = paymentId,
            Reason = "Trip cancelled"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Handle_WhenRefundAlreadyCompleted_ReturnsExistingRefundWithoutCallingStrategy()
    {
        // Arrange
        var payment = new PaymentTransactionBuilder()
            .WithStatus(PaymentStatus.Completed)
            .Build();

        var existingRefund = Refund.Create(payment.Id, payment.Amount, "Previous refund");
        existingRefund.MarkCompleted();

        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _refunds.GetCompletedByPaymentIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(existingRefund);

        var command = new RefundPaymentCommand
        {
            PaymentId = payment.Id,
            Reason = "Duplicate refund request"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.RefundId.ShouldBe(existingRefund.Id);

        await _paymentStrategy.DidNotReceive().RefundAsync(Arg.Any<RefundExecutionContext>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Refunded)]
    public async Task Handle_WhenPaymentStatusIsNotCompleted_ReturnsConflictError(PaymentStatus status)
    {
        // Arrange
        var payment = new PaymentTransactionBuilder()
            .WithStatus(status)
            .Build();

        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _refunds.GetCompletedByPaymentIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns((Refund?)null);

        var command = new RefundPaymentCommand
        {
            PaymentId = payment.Id,
            Reason = "Refund requested"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ConflictError>();
    }

    [Fact]
    public async Task Handle_WhenStrategyRefundFails_ReturnsFailureError()
    {
        // Arrange
        var payment = new PaymentTransactionBuilder()
            .WithStatus(PaymentStatus.Completed)
            .Build();

        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _refunds.GetCompletedByPaymentIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns((Refund?)null);

        const string failureReason = "Driver wallet has insufficient balance for refund";
        _paymentStrategy.RefundAsync(Arg.Any<RefundExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new ConflictError(failureReason)));

        var command = new RefundPaymentCommand
        {
            PaymentId = payment.Id,
            Reason = "Refund requested"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Message.ShouldBe(failureReason);
        _refunds.DidNotReceive().Add(Arg.Any<Refund>());
    }

    [Fact]
    public async Task Handle_WhenSuccessful_MarksRefundCompletedAndPaymentRefunded()
    {
        // Arrange
        var payment = new PaymentTransactionBuilder()
            .WithStatus(PaymentStatus.Completed)
            .Build();

        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _refunds.GetCompletedByPaymentIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns((Refund?)null);

        _paymentStrategy.RefundAsync(Arg.Any<RefundExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new RefundPaymentCommand
        {
            PaymentId = payment.Id,
            Reason = "Customer dissatisfaction"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(RefundStatus.Completed);
        payment.Status.ShouldBe(PaymentStatus.Refunded);

        _refunds.Received(1).Add(Arg.Is<Refund>(r => r.PaymentId == payment.Id && r.Status == RefundStatus.Completed));
        await _transactions.Received(1).EndTransactionAsync();
    }
}
