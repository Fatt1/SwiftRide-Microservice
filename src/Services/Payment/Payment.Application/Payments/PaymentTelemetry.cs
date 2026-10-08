using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Payment.Application.Payments;

public static class PaymentTelemetry
{
    public const string Name = "SwiftRide.Payment";
    public static readonly ActivitySource Activities = new(Name);
    private static readonly Meter Meter = new(Name);
    private static readonly Counter<long> Outcomes = Meter.CreateCounter<long>("payment.outcomes");
    public static void Record(string operation, string status)
        => Outcomes.Add(1, new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", status));
}
