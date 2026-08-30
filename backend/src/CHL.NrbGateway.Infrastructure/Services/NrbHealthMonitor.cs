using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CHL.NrbGateway.Infrastructure.Services;

/// <summary>
/// Passive NRB link monitor. Derives health from real verification traffic:
/// a call that completes (any HTTP response) is "up", a transport exception
/// (timeout / connection refused) is "down". Writes are throttled, and
/// up/down transitions create and resolve downtime incidents.
/// </summary>
public class NrbHealthMonitor : INrbHealthMonitor
{
    private static readonly TimeSpan HealthyThrottle = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan DownThrottle = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NrbHealthMonitor> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool? _lastStatus;
    private DateTimeOffset _lastRecordedAt = DateTimeOffset.MinValue;

    public NrbHealthMonitor(IServiceScopeFactory scopeFactory, ILogger<NrbHealthMonitor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RecordAsync(bool isUp, int? latencyMs, string? error, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var throttle = isUp ? HealthyThrottle : DownThrottle;

            // Throttle identical repeated signals so we don't write a row per request.
            if (_lastStatus == isUp && now - _lastRecordedAt < throttle)
                return;

            using var scope = _scopeFactory.CreateScope();
            var configDb = scope.ServiceProvider.GetRequiredService<IConfigDbContext>();

            if (_lastStatus == true && !isUp)
            {
                configDb.Add(new NrbDowntimeIncident
                {
                    Id = Guid.NewGuid(),
                    StartedAt = now,
                    EndedAt = null,
                    DetectedBy = IncidentDetectionMethod.AUTOMATIC,
                    Notified = false,
                    ResolvedBy = null
                });
                _logger.LogWarning("NRB link transitioned to DOWN ({Error})", error);
            }
            else if (_lastStatus == false && isUp)
            {
                var openIncident = configDb.NrbDowntimeIncidents
                    .Where(i => i.EndedAt == null)
                    .OrderByDescending(i => i.StartedAt)
                    .FirstOrDefault();
                if (openIncident != null)
                {
                    openIncident.EndedAt = now;
                    configDb.Update(openIncident);
                }
                _logger.LogInformation("NRB link recovered.");
            }

            configDb.Add(new NrbHealthCheck
            {
                Id = Guid.NewGuid(),
                CheckedAt = now,
                IsUp = isUp,
                LatencyMs = latencyMs,
                ErrorMessage = error
            });

            await configDb.SaveChangesAsync(cancellationToken);

            _lastStatus = isUp;
            _lastRecordedAt = now;
        }
        finally
        {
            _gate.Release();
        }
    }
}
