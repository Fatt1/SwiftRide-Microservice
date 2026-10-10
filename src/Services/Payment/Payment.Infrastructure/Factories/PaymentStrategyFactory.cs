using Payment.Application.Abstractions;
using Shared.Enums.Payments;

namespace Payment.Infrastructure.Factories;

public sealed class PaymentStrategyFactory : IPaymentStrategyFactory
{
    private readonly IReadOnlyDictionary<PaymentMethod, IPaymentStrategy> _strategies;

    public PaymentStrategyFactory(IEnumerable<IPaymentStrategy> strategies)
    {
        var registered = strategies.ToList();
        if (registered.GroupBy(x => x.Method).Any(x => x.Count() != 1))
            throw new InvalidOperationException("Duplicate payment strategy registration.");
        _strategies = registered.ToDictionary(x => x.Method);
    }

    public IPaymentStrategy GetStrategy(PaymentMethod method) =>
        _strategies.TryGetValue(method, out var strategy)
            ? strategy : throw new ArgumentOutOfRangeException(nameof(method), "Unsupported payment method.");
}
