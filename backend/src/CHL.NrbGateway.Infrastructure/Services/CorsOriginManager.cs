using System.Collections.Concurrent;
using CHL.NrbGateway.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CHL.NrbGateway.Infrastructure.Services;

public class CorsOriginManager : ICorsOriginManager
{
    private readonly ConcurrentDictionary<string, byte> _allowedOrigins = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<CorsOriginManager> _logger;

    public CorsOriginManager(ILogger<CorsOriginManager> logger)
    {
        _logger = logger;
    }

    public bool IsOriginAllowed(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin)) return false;

        var normalized = NormalizeOrigin(origin);

        // Always allow localhost in development / debugging
        if (normalized.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("https://localhost:", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("http://localhost", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("https://localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return _allowedOrigins.ContainsKey(normalized);
    }

    public void Reload(IEnumerable<string> origins)
    {
        _allowedOrigins.Clear();
        foreach (var origin in origins)
        {
            if (!string.IsNullOrWhiteSpace(origin))
            {
                var norm = NormalizeOrigin(origin);
                _allowedOrigins.TryAdd(norm, 0);
            }
        }
        _logger.LogInformation("CORS origins reloaded: {Count} active origins.", _allowedOrigins.Count);
    }

    public void AddOrEnable(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin)) return;
        var norm = NormalizeOrigin(origin);
        _allowedOrigins.TryAdd(norm, 0);
        _logger.LogInformation("Dynamic CORS origin added/enabled: {Origin}", norm);
    }

    public void Remove(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin)) return;
        var norm = NormalizeOrigin(origin);
        _allowedOrigins.TryRemove(norm, out _);
        _logger.LogInformation("Dynamic CORS origin removed/disabled: {Origin}", norm);
    }

    public IReadOnlyCollection<string> GetActiveOrigins() =>
        _allowedOrigins.Keys.ToList();

    private static string NormalizeOrigin(string origin) =>
        origin.Trim().TrimEnd('/');
}
