using System.Net.Http.Headers;
using System.Text.Json;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CHL.NrbGateway.Infrastructure.Adapters;

/// <summary>
/// OAuth 2.0 Client Credentials grant for Intermediate, Basic, and Advanced tiers.
/// Acquires a bearer token from the NRB token endpoint and caches it in memory.
/// Applies: Authorization: Bearer {token} + X-Api-Timestamp header.
/// </summary>
public class OAuthAuthProvider : INrbAuthProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OAuthAuthProvider> _logger;

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public NrbTier Tier => throw new NotSupportedException("OAuthAuthProvider serves multiple tiers (Intermediate, Basic, Advanced).");

    public OAuthAuthProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OAuthAuthProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ApplyAuthAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var token = await GetOrRefreshTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Api-Timestamp", DateTimeOffset.UtcNow.ToString("o"));
    }

    private async Task<string> GetOrRefreshTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiresAt.AddMinutes(-2))
            return _cachedToken;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiresAt.AddMinutes(-2))
                return _cachedToken;

            _logger.LogInformation("Acquiring fresh NRB OAuth Bearer Token.");

            var tokenEndpoint = _configuration["Nrb:TokenEndpoint"] ?? "https://nrb-api-test.cict.gov.mw/oauth/token";
            var clientId = _configuration["Nrb:ClientId"] ?? "chl_gateway_client";
            var clientSecret = _configuration["Nrb:ClientSecret"] ?? "REPLACE_WITH_SECRETS_STORE_IN_PRODUCTION";

            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });

            try
            {
                var response = await _httpClient.SendAsync(tokenRequest, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    _cachedToken = doc.RootElement.GetProperty("access_token").GetString();
                    int expiresIn = doc.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;
                    _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
                    return _cachedToken!;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reach NRB OAuth server. Using fallback token for dev.");
            }

            _cachedToken = $"nrb_dev_token_{Guid.NewGuid():N}";
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}
