using Shared.Enums.Payments;

namespace Payment.Application.Abstractions;

public interface IPaymentStrategyFactory
{
    IPaymentStrategy GetStrategy(PaymentMethod method);
}
