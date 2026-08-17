using CHL.NrbGateway.Domain.Enums;

namespace CHL.NrbGateway.Application.Common.Interfaces;

/// <summary>
/// Provides NRB authentication for a specific tier. Each tier may use a
/// different auth mechanism (OAuth Bearer vs ClientId/ClientKey headers).
/// </summary>
public interface INrbAuthProvider
{
    NrbTier Tier { get; }

    /// <summary>Apply authentication to an outgoing HTTP request.</summary>
    Task ApplyAuthAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}
