using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.Services;
using CHL.NrbGateway.Infrastructure.Adapters;
using CHL.NrbGateway.Infrastructure.Persistence;
using CHL.NrbGateway.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CHL.NrbGateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. KycDbContext (schema "kyc", Postgres role "gateway_role")
        var kycConnectionString = configuration.GetConnectionString("Kyc");
        services.AddDbContext<KycDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(kycConnectionString))
            {
                options.UseNpgsql(kycConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "kyc");
                });
            }
            else
            {
                options.UseInMemoryDatabase("CHL_KycDb_Dev");
            }
        });
        services.AddScoped<IKycDbContext>(provider => provider.GetRequiredService<KycDbContext>());

        // 2. ConfigDbContext (schema "config", Postgres role "portal_role")
        var configConnectionString = configuration.GetConnectionString("Config");
        services.AddDbContext<ConfigDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(configConnectionString))
            {
                options.UseNpgsql(configConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "config");
                });
            }
            else
            {
                options.UseInMemoryDatabase("CHL_ConfigDb_Dev");
            }
        });
        services.AddScoped<IConfigDbContext>(provider => provider.GetRequiredService<ConfigDbContext>());

        // 3. Cryptography & Auth Services
        services.AddSingleton<IHmacService, HmacService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IApiKeyValidationService, ApiKeyValidationService>();

        // 3b. Object storage (MinIO / S3-compatible)
        services.AddSingleton<IBlobStorageService, MinioBlobStorageService>();

        // 4. NRB Auth Providers (split by auth mechanism)
        services.AddSingleton<OAuthAuthProvider>();
        services.AddSingleton<ClientKeyAuthProvider>();

        // 5. NRB Tier Adapter (HttpClient for OAuth token refresh + NRB API calls)
        services.AddHttpClient<INrbTierAdapter, NrbIntermediateAdapter>();

        // 6. Verification Application Service
        services.AddScoped<IVerificationService, VerificationService>();

        return services;
    }
}
