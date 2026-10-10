using System.Collections.Concurrent;
using Payment.Application.Payments;

namespace Payment.Infrastructure.Gateway;

public sealed class MockPaymentGateway : IPaymentGateway
{
    private sealed record Operation(decimal Amount, string Scenario, GatewayResult Result);
    private readonly ConcurrentDictionary<Guid, Operation> charges = new();

    public Task<GatewayResult?> LookupAsync(Guid key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<GatewayResult?>(charges.TryGetValue(key, out var operation)
            ? operation.Result : null);
    }

    public Task<GatewayResult> ChargeAsync(Guid key, string token, decimal amount, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (token == "mock-timeout") throw new TimeoutException("Mock gateway unavailable.");
        var candidate = new Operation(amount, token, new(token != "mock-declined", Guid.NewGuid().ToString()));
        var operation = charges.GetOrAdd(key, candidate);
        if (operation.Amount != amount || operation.Scenario != token)
            throw new InvalidOperationException("Gateway key payload mismatch.");
        if (ReferenceEquals(operation, candidate) && token == "mock-charge-response-lost")
            throw new TimeoutException("Mock response lost after charge.");
        return Task.FromResult(operation.Result);
    }

}
