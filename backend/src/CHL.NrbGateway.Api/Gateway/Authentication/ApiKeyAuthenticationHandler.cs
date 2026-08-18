using System.Security.Claims;
using System.Text.Encodings.Web;
using CHL.NrbGateway.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CHL.NrbGateway.Api.Gateway.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyValidationService _apiKeyValidationService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidationService apiKeyValidationService)
        : base(options, logger, encoder)
    {
        _apiKeyValidationService = apiKeyValidationService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var apiKeyHeaderValues))
        {
            return AuthenticateResult.Fail("Missing X-Api-Key Header.");
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return AuthenticateResult.Fail("Invalid X-Api-Key Header.");
        }

        var validationResult = await _apiKeyValidationService.ValidateApiKeyAsync(providedApiKey, Context.RequestAborted);

        if (!validationResult.IsValid || !validationResult.ProjectId.HasValue)
        {
            return AuthenticateResult.Fail("Invalid or revoked API key.");
        }

        var claims = new[]
        {
            new Claim("ProjectId", validationResult.ProjectId.Value.ToString()),
            new Claim("ProjectShortCode", validationResult.ProjectShortCode ?? string.Empty),
            new Claim("ProjectName", validationResult.ProjectName ?? string.Empty),
            new Claim(ClaimTypes.Role, "Project")
        };

        var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationOptions.DefaultScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationOptions.DefaultScheme);

        return AuthenticateResult.Success(ticket);
    }
}
