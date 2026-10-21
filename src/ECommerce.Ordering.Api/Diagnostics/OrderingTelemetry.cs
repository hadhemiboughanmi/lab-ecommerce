using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ECommerce.Ordering.Api.Diagnostics;

/// <summary>
/// Business telemetry of the Ordering service: what the technical metrics cannot tell
/// (how many orders, how many rejected and why, how much they are worth).
/// Exported through OpenTelemetry by ServiceDefaults (meters and sources "ECommerce.*").
/// </summary>
public sealed class OrderingTelemetry : IDisposable
{
    public const string Name = "ECommerce.Ordering";

    public static readonly ActivitySource ActivitySource = new(Name);

    private readonly Meter _meter;

    public OrderingTelemetry(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(Name);

        OrdersCreated = _meter.CreateCounter<long>(
            "ecommerce.orders.created", unit: "{order}",
            description: "Orders confirmed and persisted.");

        OrdersRejected = _meter.CreateCounter<long>(
            "ecommerce.orders.rejected", unit: "{order}",
            description: "Orders refused, tagged with the rejection reason.");

        OrderAmount = _meter.CreateHistogram<double>(
            "ecommerce.order.amount", unit: "EUR",
            description: "Total amount of confirmed orders.");
    }

    public Counter<long> OrdersCreated { get; }
    public Counter<long> OrdersRejected { get; }
    public Histogram<double> OrderAmount { get; }

    /// <summary>Counts a refused order. <paramref name="reason"/> must stay low-cardinality.</summary>
    public void Reject(string reason) =>
        OrdersRejected.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void Dispose() => _meter.Dispose();
}
