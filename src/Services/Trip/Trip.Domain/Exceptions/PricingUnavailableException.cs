namespace Trip.Domain.Exceptions;

public sealed class PricingUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
