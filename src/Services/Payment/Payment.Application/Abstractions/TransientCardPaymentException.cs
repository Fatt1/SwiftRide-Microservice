namespace Payment.Application.Abstractions;

public sealed class TransientCardPaymentException(string message) : Exception(message);
