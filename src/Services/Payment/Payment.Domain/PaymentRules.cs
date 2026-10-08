using Shared.Exceptions;

namespace Payment.Domain;

internal static class PaymentRules
{
    public static void Require(bool condition, string message)
    {
        if (!condition) throw new DomainException(message);
    }

    public static void Amount(decimal amount, bool allowZero = false)
        => Require((allowZero ? amount >= 0 : amount > 0) && decimal.Truncate(amount) == amount,
            "VND amount must be a whole number and must not be negative or zero unless allowed.");

    public static void Id(Guid id) => Require(id != Guid.Empty, "Id must not be empty.");

    public static void Currency(string currency)
        => Require(currency == "VND", "Only VND is supported.");
}
