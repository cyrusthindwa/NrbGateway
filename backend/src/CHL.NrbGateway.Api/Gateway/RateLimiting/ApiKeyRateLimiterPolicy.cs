using System.Threading.RateLimiting;
using CHL.NrbGateway.Api.Gateway.Authentication;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace CHL.NrbGateway.Api.Gateway.RateLimiting;

/// <summary>
/// Per-project rate limiter for the Gateway (X-Api-Key) surface.
/// Each API key gets its own token bucket sized by the key's own
/// <c>RateLimitPerMinute</c> (config.project_api_keys), replenished once per
/// minute. On breach the request is rejected with HTTP 429 and a Retry-After
/// header. The limiter runs before authentication, so it also throttles
/// brute-force attempts against the key header itself.
/// </summary>
public sealed class ApiKeyRateLimiterPolicy : IRateLimiterPolicy<string>
{
    public const string PolicyName = "PerApiKey";

    private const int DefaultRateLimitPerMinute = 100;

    private readonly IConfigDbContext _configDbContext;
    private readonly IApiKeyValidationService _apiKeyValidationService;
    private readonly ILogger<ApiKeyRateLimiterPolicy> _logger;

    /// <summary>Handles requests rejected by the rate limiter: logs and sets Retry-After; the middleware writes the 429.</summary>
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; }

    public ApiKeyRateLimiterPolicy(
        IConfigDbContext configDbContext,
        IApiKeyValidationService apiKeyValidationService,
        ILogger<ApiKeyRateLimiterPolicy> logger)
    {
        _configDbContext = configDbContext;
        _apiKeyValidationService = apiKeyValidationService;
        _logger = logger;

        OnRejected = (context, _) =>
        {
            var apiKey = context.HttpContext.Request.Headers[ApiKeyAuthenticationOptions.HeaderName].FirstOrDefault() ?? string.Empty;
            _logger.LogWarning("Gateway rate limit exceeded for partition {Partition}.", _apiKeyValidationService.HashApiKey(apiKey));

            context.HttpContext.Response.Headers.RetryAfter = "60";
            return ValueTask.CompletedTask;
        };
    }

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var apiKey = httpContext.Request.Headers[ApiKeyAuthenticationOptions.HeaderName].FirstOrDefault() ?? string.Empty;

        // Partition on the hashed key so the raw key never becomes an in-memory partition key.
        var partitionKey = _apiKeyValidationService.HashApiKey(apiKey);

        // The options factory runs once per partition (per API key), then the limiter
        // instance is cached by the RateLimiterManager until the partition goes idle.
        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey,
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = ResolveRateLimit(apiKey),
                TokensPerPeriod = ResolveRateLimit(apiKey),
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                AutoReplenishment = true,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    }

    /// <summary>
    /// Reads the live <see cref="ProjectApiKey.RateLimitPerMinute"/> for the presented key.
    /// Unknown/revoked keys get the default limit — authentication rejects them anyway.
    /// </summary>
    private int ResolveRateLimit(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return DefaultRateLimitPerMinute;

        var hash = _apiKeyValidationService.HashApiKey(apiKey);
        var entry = _configDbContext.ProjectApiKeys
            .FirstOrDefault(k => k.KeyHash == hash && k.Status == ApiKeyStatus.ACTIVE);

        return Math.Max(1, entry?.RateLimitPerMinute ?? DefaultRateLimitPerMinute);
    }
}
