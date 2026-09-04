using System.Text;
using System.Text.Json.Serialization;
using CHL.NrbGateway.Api.Gateway.Authentication;
using CHL.NrbGateway.Api.Gateway.Middleware;
using CHL.NrbGateway.Api.Gateway.RateLimiting;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;
using CHL.NrbGateway.Infrastructure;
using CHL.NrbGateway.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

// 0. Load .env for local development so secrets stay out of appsettings / source control.
if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
{
    LoadDotEnv(Directory.GetCurrentDirectory());
}

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Serilog structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Add Infrastructure Services
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. Add Authentication (API Key for Gateway + JWT Bearer for Portal)
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Missing Jwt:SecretKey configuration.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = ApiKeyAuthenticationOptions.DefaultScheme;
})
.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
    ApiKeyAuthenticationOptions.DefaultScheme, null)
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CHL_NRB_Gateway",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CHL_Portal_Admins",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
});

// CORS: dynamic allow-list managed via ICorsOriginManager and persisted in config.cors_origins.
ICorsOriginManager? corsOriginManager = null;

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => corsOriginManager?.IsOriginAllowed(origin) ?? true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// HSTS + HTTPS redirection. HSTS headers are only emitted outside Development;
// the redirect target port is configurable via Https:Port.
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});
builder.Services.AddHttpsRedirection(options =>
{
    if (int.TryParse(builder.Configuration["Https:Port"], out var httpsPort) && httpsPort > 0)
        options.HttpsPort = httpsPort;
});

// Per-API-key rate limiting for the Gateway surface (HTTP 429 on breach).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy<string, ApiKeyRateLimiterPolicy>(ApiKeyRateLimiterPolicy.PolicyName);
});

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// 4. Configure Swagger / OpenAPI (Development only)
// Microsoft.OpenApi v2.7.5 (used by Swashbuckle 10.x) puts all types in the root Microsoft.OpenApi namespace
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CHL NRB Verification Gateway API",
        Version = "v1",
        Description = "Continental Holdings Limited NRB Verification Gateway & Admin Portal (CICT/10032601/NRB)"
    });

    // Define security for API Key (Gateway endpoints)
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Project API Key. Set header: X-Api-Key: chl_test_... or chl_live_...",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    // Define security for JWT Bearer (Portal endpoints)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token. Set header: Authorization: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Swashbuckle 10.x AddSecurityRequirement takes Func<OpenApiDocument, OpenApiSecurityRequirement>
    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("ApiKey"),
            new List<string>()
        },
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

corsOriginManager = app.Services.GetRequiredService<ICorsOriginManager>();

// 5. HTTP Pipeline
app.UseMiddleware<GlobalErrorHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CHL NRB Gateway API v1"));
}
else
{
    // HSTS header (max-age 1y, includeSubDomains, preload) — never sent in Development.
    app.UseHsts();
}

// Redirect HTTP → HTTPS. Only active once an HTTPS port is reachable (see Https:Port).
app.UseHttpsRedirection();

app.UseSerilogRequestLogging();
app.UseRouting();
app.UseRateLimiter(); // enforces the per-API-key policies on gateway endpoints (429 on breach)
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 6. Dev database seeding via concrete DbContext (uses EF DbContext.Add directly)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var configDb = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyValidationService>();

        bool isInMemory = configDb.Database.IsInMemory();
        bool canConnect = !isInMemory && configDb.Database.CanConnect();

        // Apply pending EF migrations on startup so the full docker-compose
        // stack comes up with a single `docker compose up`.
        if (!isInMemory && canConnect)
        {
            configDb.Database.Migrate();
            var kycDb = scope.ServiceProvider.GetRequiredService<KycDbContext>();
            kycDb.Database.Migrate();

            configDb.Database.ExecuteSqlRaw(@"ALTER TABLE config.projects ADD COLUMN IF NOT EXISTS ""ProjectType"" TEXT NOT NULL DEFAULT 'SYSTEM_INTEGRATION';");

            try
            {
                configDb.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS config.cors_origins (
                        ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        ""Origin"" TEXT NOT NULL UNIQUE,
                        ""Description"" TEXT NULL,
                        ""IsEnabled"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        ""UpdatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW()
                    );
                ");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Raw DDL table creation for cors_origins skipped, ensuring created via EF Core...");
            }

            var manualDb = scope.ServiceProvider.GetRequiredService<ManualPortalDbContext>();
            try
            {
                manualDb.Database.ExecuteSqlRaw(@"
                    CREATE SCHEMA IF NOT EXISTS verification_portal;
                    CREATE TABLE IF NOT EXISTS verification_portal.manual_users (
                        ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        ""CompanyId"" UUID NOT NULL REFERENCES config.companies(""Id"") ON DELETE CASCADE,
                        ""Email"" TEXT NOT NULL UNIQUE,
                        ""PasswordHash"" TEXT NOT NULL,
                        ""Status"" TEXT NOT NULL DEFAULT 'ACTIVE',
                        ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        ""LastLoginAt"" TIMESTAMPTZ NULL,
                        ""MustChangePassword"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""PasswordResetTokenHash"" TEXT NULL,
                        ""PasswordResetExpiresAt"" TIMESTAMPTZ NULL
                    );
                    CREATE TABLE IF NOT EXISTS verification_portal.manual_verification_log (
                        ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        ""ManualUserId"" UUID NOT NULL REFERENCES verification_portal.manual_users(""Id"") ON DELETE CASCADE,
                        ""CompanyId"" UUID NOT NULL REFERENCES config.companies(""Id"") ON DELETE CASCADE,
                        ""NationalIdMasked"" TEXT NOT NULL,
                        ""ResultStatus"" TEXT NOT NULL,
                        ""GatewayRequestId"" UUID NULL REFERENCES kyc.gateway_requests(""Id"") ON DELETE SET NULL,
                        ""RequestedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW()
                    );
                    CREATE TABLE IF NOT EXISTS verification_portal.manual_user_otp_codes (
                        ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        ""ManualUserId"" UUID NOT NULL REFERENCES verification_portal.manual_users(""Id"") ON DELETE CASCADE,
                        ""CodeHash"" TEXT NOT NULL,
                        ""ExpiresAt"" TIMESTAMPTZ NOT NULL,
                        ""Used"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""AttemptCount"" INT NOT NULL DEFAULT 0,
                        ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW()
                    );

                    ALTER TABLE verification_portal.manual_users ADD COLUMN IF NOT EXISTS ""MustChangePassword"" BOOLEAN NOT NULL DEFAULT TRUE;
                    ALTER TABLE verification_portal.manual_users ADD COLUMN IF NOT EXISTS ""PasswordResetTokenHash"" TEXT NULL;
                    ALTER TABLE verification_portal.manual_users ADD COLUMN IF NOT EXISTS ""PasswordResetExpiresAt"" TIMESTAMPTZ NULL;
                ");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Raw DDL table creation skipped, ensuring created via EF Core...");
                manualDb.Database.EnsureCreated();
            }
        }

        if (isInMemory || canConnect)
        {
            if (!configDb.AdminUsers.Any())
            {
                var admin = new AdminUser
                {
                    Id = Guid.NewGuid(),
                    Name = "C. Thindwa (ICT)",
                    Email = "cthindwa@continental.mw",
                    PasswordHash = passwordHasher.HashPassword("password"),
                    Status = AdminStatus.ACTIVE,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                configDb.Add(admin);
                configDb.SaveChanges();

                var cdhCompany = new Company { Id = Guid.NewGuid(), Name = "CDH Investment Bank", ShortCode = "CDHIB", CreatedAt = DateTimeOffset.UtcNow };
                var camCompany = new Company { Id = Guid.NewGuid(), Name = "Continental Asset Management", ShortCode = "CAM", CreatedAt = DateTimeOffset.UtcNow };
                configDb.Add(cdhCompany);
                configDb.Add(camCompany);
                configDb.SaveChanges();

                var cdhProject = new Project { Id = Guid.NewGuid(), CompanyId = cdhCompany.Id, Name = "CDH Investment Bank — Gateway", ShortCode = "CDHIB", ProjectType = "SYSTEM_INTEGRATION", CreatedAt = DateTimeOffset.UtcNow };
                var cdhManualProject = new Project { Id = Guid.NewGuid(), CompanyId = cdhCompany.Id, Name = "Manual Verification Interface", ShortCode = "CDH-MAN", ProjectType = "MANUAL_PORTAL", CreatedAt = DateTimeOffset.UtcNow };
                var camProject = new Project { Id = Guid.NewGuid(), CompanyId = camCompany.Id, Name = "CAM — Gateway", ShortCode = "CAM", ProjectType = "SYSTEM_INTEGRATION", CreatedAt = DateTimeOffset.UtcNow };
                configDb.Add(cdhProject);
                configDb.Add(cdhManualProject);
                configDb.Add(camProject);
                configDb.SaveChanges();

                // Dev seed API Key for CDH Investment Bank
                var devKey = "chl_test_cdhib_dev_key_12345";
                configDb.Add(new ProjectApiKey
                {
                    Id = Guid.NewGuid(),
                    ProjectId = cdhProject.Id,
                    KeyHash = apiKeyService.HashApiKey(devKey),
                    KeyPrefix = devKey[..12],
                    Status = ApiKeyStatus.ACTIVE,
                    RateLimitPerMinute = 100,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = admin.Id
                });

                var devManualKey = "sec_live_cdh_manual_key_12345";
                configDb.Add(new ProjectApiKey
                {
                    Id = Guid.NewGuid(),
                    ProjectId = cdhManualProject.Id,
                    KeyHash = apiKeyService.HashApiKey(devManualKey),
                    KeyPrefix = devManualKey[..12],
                    Status = ApiKeyStatus.ACTIVE,
                    RateLimitPerMinute = 1000,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = admin.Id
                });

                // Default tier settings — ensure TEXT_LOOKUP is enabled
                configDb.Add(new VerificationTierSetting { Tier = NrbTier.BASIC, Enabled = true, CostPerRequest = 20m, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = admin.Id });
                configDb.Add(new VerificationTierSetting { Tier = NrbTier.TEXT_LOOKUP, Enabled = true, CostPerRequest = 50m, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = admin.Id });
                configDb.Add(new VerificationTierSetting { Tier = NrbTier.INTERMEDIATE, Enabled = true, CostPerRequest = 30m, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = admin.Id });
                configDb.Add(new VerificationTierSetting { Tier = NrbTier.ADVANCED, Enabled = true, CostPerRequest = 100m, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = admin.Id });
                configDb.SaveChanges();
            }

            // Unconditional check for Manual Portal User and Internal Project
            var manualDb = scope.ServiceProvider.GetRequiredService<ManualPortalDbContext>();
            var company = configDb.Companies.FirstOrDefault(c => c.ShortCode == "CDHIB");
            if (company != null)
            {
                var manualProj = configDb.Projects.FirstOrDefault(p => p.CompanyId == company.Id && p.ProjectType == "MANUAL_PORTAL");
                if (manualProj == null)
                {
                    manualProj = new Project
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = company.Id,
                        Name = "Manual Verification Interface",
                        ShortCode = "CDH-MAN",
                        ProjectType = "MANUAL_PORTAL",
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    configDb.Add(manualProj);
                    configDb.SaveChanges();
                }

                if (!manualDb.ManualUsers.Any(u => u.Email == "agent@cdhbank.mw"))
                {
                    manualDb.Add(new CHL.NrbGateway.Domain.Entities.ManualPortal.ManualUser
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = company.Id,
                        Email = "agent@cdhbank.mw",
                        PasswordHash = passwordHasher.HashPassword("Password123!"),
                        Status = "ACTIVE",
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                    manualDb.SaveChanges();
                    Log.Information("Dev seed manual portal user created: agent@cdhbank.mw / Password123!");
                }
            }

            // Default notification channel (email alerts)
            if (!configDb.NotificationChannels.Any())
            {
                var adminUser = configDb.AdminUsers.FirstOrDefault();
                var adminId = adminUser?.Id ?? Guid.Empty;
                configDb.Add(new NotificationChannel
                {
                    Id = Guid.NewGuid(),
                    ChannelType = NotificationChannelType.EMAIL,
                    Target = "cthindwa@continental.mw",
                    Enabled = true,
                    CreatedBy = adminId,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                configDb.SaveChanges();
            }

            // Default CORS origins
            if (!configDb.CorsOrigins.Any())
            {
                var defaultCorsOrigins = (builder.Configuration["Cors:AllowedOrigins"]
                        ?? "http://localhost:3000,http://localhost:3001,http://kyc-dev.continental.mw,http://manual.kyc-dev.continental.mw")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var orig in defaultCorsOrigins)
                {
                    configDb.Add(new CorsOrigin
                    {
                        Id = Guid.NewGuid(),
                        Origin = orig.Trim().TrimEnd('/'),
                        Description = "Default system origin",
                        IsEnabled = true,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });
                }
                configDb.SaveChanges();
            }

            // Sync active origins to dynamic CORS manager
            var activeCorsOrigins = configDb.CorsOrigins
                .Where(c => c.IsEnabled)
                .Select(c => c.Origin)
                .ToList();
            corsOriginManager.Reload(activeCorsOrigins);
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Dev database seeding was skipped or failed.");
    }
}

app.Run();

// Loads a .env file (repo root or any ancestor of the current directory) into
// environment variables for local development. Values map to .NET configuration
// keys via the "__" separator convention (e.g. MAIL_PASSWORD -> Mail__Password).
static void LoadDotEnv(string startDirectory)
{
    var envFile = FindDotEnv(startDirectory);
    if (envFile is null)
        return;

    foreach (var rawLine in File.ReadLines(envFile))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
            continue;

        var separator = line.IndexOf('=');
        if (separator <= 0)
            continue;

        var key = line[..separator].Trim();
        var value = line[(separator + 1)..].Trim();

        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            value = value[1..^1];

        if (key.Length == 0)
            continue;

        var envName = ToEnvironmentVariableName(key);
        if (Environment.GetEnvironmentVariable(envName) is null)
            Environment.SetEnvironmentVariable(envName, value);
    }
}

static string? FindDotEnv(string startDirectory)
{
    var directory = new DirectoryInfo(startDirectory);
    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, ".env");
        if (File.Exists(candidate))
            return candidate;
        directory = directory.Parent;
    }

    return null;
}

static string ToEnvironmentVariableName(string envKey)
{
    var parts = envKey.Split('_', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0)
        return envKey;

    static string Pascal(string value) =>
        char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();

    var section = Pascal(parts[0]);
    var suffix = string.Concat(parts.Skip(1).Select(Pascal));
    return suffix.Length == 0 ? section : $"{section}__{suffix}";
}

// Required by xUnit WebApplicationFactory
public partial class Program { }
