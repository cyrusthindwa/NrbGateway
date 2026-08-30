namespace CHL.NrbGateway.Application.Common.Interfaces;

/// <summary>
/// Records passive NRB link health derived from real verification traffic.
/// Implementations throttle writes and maintain downtime incidents.
/// </summary>
public interface INrbHealthMonitor
{
    Task RecordAsync(bool isUp, int? latencyMs, string? error, CancellationToken cancellationToken = default);
}
