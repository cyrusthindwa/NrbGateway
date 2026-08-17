using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CHL.NrbGateway.Infrastructure.Adapters;

/// <summary>
/// ClientId/ClientKey custom header authentication for the Text Lookup tier.
/// NRB Text Lookup does NOT use OAuth; it expects literal headers:
///   ClientId: {value}
///   ClientKey: {value}
/// Note: no "X-" prefix — exact casing matters per NRB API docs.
/// </summary>
public class ClientKeyAuthProvider : INrbAuthProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClientKeyAuthProvider> _logger;

    public NrbTier Tier => NrbTier.TEXT_LOOKUP;

    public ClientKeyAuthProvider(
        IConfiguration configuration,
        ILogger<ClientKeyAuthProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task ApplyAuthAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var clientId = _configuration["Nrb:TextLookupClientId"] ?? "chl_gateway_text_lookup_dev";
        var clientKey = _configuration["Nrb:TextLookupClientKey"] ?? "REPLACE_WITH_SECRETS_STORE_IN_PRODUCTION";

        request.Headers.Add("ClientId", clientId);
        request.Headers.Add("ClientKey", clientKey);

        _logger.LogDebug("Applied ClientId/ClientKey headers for Text Lookup request.");
        return Task.CompletedTask;
    }
}
