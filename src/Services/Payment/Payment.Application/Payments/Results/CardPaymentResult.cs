namespace Payment.Application.Payments.Results;

public sealed record CardPaymentResult(bool IsSuccess, string Response, string? FailureReason = null);
