using CHL.NrbGateway.Domain.Entities.Config;

namespace CHL.NrbGateway.Application.Common.Interfaces;

public record SubsidiaryApiKeyValidationResult(
    bool IsValid,
    Guid? SubsidiaryId,
    string? SubsidiaryShortCode,
    string? SubsidiaryName,
    int RateLimitPerMinute
);

public interface IApiKeyValidationService
{
    Task<SubsidiaryApiKeyValidationResult> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    string HashApiKey(string plaintextKey);
    (string plaintextKey, string prefix, string hash) GenerateApiKey();
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public interface IJwtTokenService
{
    string GenerateToken(AdminUser adminUser);
}
