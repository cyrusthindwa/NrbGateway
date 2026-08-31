using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CHL.NrbGateway.Infrastructure.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash)) return false;
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(AdminUser adminUser)
    {
        var secret = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Missing Jwt:SecretKey configuration.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, adminUser.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, adminUser.Email),
            new Claim(ClaimTypes.Name, adminUser.Name),
            new Claim(ClaimTypes.Role, "CHL_ICT_Admin")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "CHL_NRB_Gateway",
            audience: _configuration["Jwt:Audience"] ?? "CHL_Portal_Admins",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateManualUserToken(CHL.NrbGateway.Domain.Entities.ManualPortal.ManualUser manualUser)
    {
        var secret = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Missing Jwt:SecretKey configuration.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, manualUser.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, manualUser.Email),
            new Claim("CompanyId", manualUser.CompanyId.ToString()),
            new Claim(ClaimTypes.Role, "Manual_Portal_User")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "CHL_NRB_Gateway",
            audience: _configuration["Jwt:Audience"] ?? "CHL_Portal_Admins",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class ApiKeyValidationService : IApiKeyValidationService
{
    private readonly IConfigDbContext _configDbContext;

    public ApiKeyValidationService(IConfigDbContext configDbContext)
    {
        _configDbContext = configDbContext;
    }

    public string HashApiKey(string plaintextKey)
    {
        if (string.IsNullOrEmpty(plaintextKey)) return string.Empty;
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plaintextKey));
        return Convert.ToHexStringLower(bytes);
    }

    public (string plaintextKey, string prefix, string hash) GenerateApiKey(ApiKeyEnvironment environment)
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var rawBase64 = WebEncoders.Base64UrlEncode(randomBytes);

        var keyPrefix = environment == ApiKeyEnvironment.LIVE ? "chl_live_" : "chl_test_";
        var plaintextKey = $"{keyPrefix}{rawBase64}";
        var prefix = plaintextKey[..12];
        var hash = HashApiKey(plaintextKey);

        return (plaintextKey, prefix, hash);
    }

    public async Task<ProjectApiKeyValidationResult> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ProjectApiKeyValidationResult(false, null, null, null, 0);
        }

        var keyHash = HashApiKey(apiKey);

        var apiKeyEntry = await _configDbContext.ProjectApiKeys
            .Include(k => k.Project)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.Status == ApiKeyStatus.ACTIVE, cancellationToken);

        if (apiKeyEntry == null || apiKeyEntry.Project == null)
        {
            return new ProjectApiKeyValidationResult(false, null, null, null, 0);
        }

        return new ProjectApiKeyValidationResult(
            IsValid: true,
            ProjectId: apiKeyEntry.ProjectId,
            ProjectShortCode: apiKeyEntry.Project.ShortCode,
            ProjectName: apiKeyEntry.Project.Name,
            RateLimitPerMinute: apiKeyEntry.RateLimitPerMinute
        );
    }
}
