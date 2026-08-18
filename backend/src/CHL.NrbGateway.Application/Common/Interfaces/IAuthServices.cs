using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;

namespace CHL.NrbGateway.Application.Common.Interfaces;

public record ProjectApiKeyValidationResult(
    bool IsValid,
    Guid? ProjectId,
    string? ProjectShortCode,
    string? ProjectName,
    int RateLimitPerMinute
);

public interface IApiKeyValidationService
{
    Task<ProjectApiKeyValidationResult> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    string HashApiKey(string plaintextKey);
    (string plaintextKey, string prefix, string hash) GenerateApiKey(ApiKeyEnvironment environment);
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
