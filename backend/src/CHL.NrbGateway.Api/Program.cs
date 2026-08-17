using System.Text;
using System.Text.Json.Serialization;
using CHL.NrbGateway.Api.Gateway.Authentication;
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
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? "DEFAULT_DEV_JWT_SECRET_KEY_REPLACE_WITH_SECRETS_STORE_IN_PRODUCTION_MIN_256_BITS";

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
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
        Description = "Subsidiary API Key. Set header: X-Api-Key: chl_live_...",
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

// 5. HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CHL NRB Gateway API v1"));
}

app.UseSerilogRequestLogging();
app.UseRouting();
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

                var cdhBank = new Subsidiary { Id = Guid.NewGuid(), Name = "CDH Investment Bank", ShortCode = "CDHIB", CreatedAt = DateTimeOffset.UtcNow };
                var cdhCap = new Subsidiary { Id = Guid.NewGuid(), Name = "Continental Capital", ShortCode = "CDHCAP", CreatedAt = DateTimeOffset.UtcNow };
                var cam = new Subsidiary { Id = Guid.NewGuid(), Name = "Continental Asset Management", ShortCode = "CAM", CreatedAt = DateTimeOffset.UtcNow };
                configDb.Add(cdhBank);
                configDb.Add(cdhCap);
                configDb.Add(cam);
                configDb.SaveChanges();

                // Dev seed API Key for CDH Investment Bank
                var devKey = "chl_live_cdhib_dev_key_12345";
                configDb.Add(new SubsidiaryApiKey
                {
                    Id = Guid.NewGuid(),
                    SubsidiaryId = cdhBank.Id,
                    KeyHash = apiKeyService.HashApiKey(devKey),
                    KeyPrefix = devKey[..12],
                    Status = ApiKeyStatus.ACTIVE,
                    RateLimitPerMinute = 100,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = admin.Id
                });

                // Default tier settings — only INTERMEDIATE is enabled for MVP
                configDb.Add(new VerificationTierSetting { Tier = NrbTier.BASIC, Enabled = false, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = admin.Id });
                configDb.Add(new VerificationTierSetting { Tier = NrbTier.TEXT_LOOKUP, Enabled = false, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = admin.Id });
                configDb.Add(new VerificationTierSetting { Tier = NrbTier.INTERMEDIATE, Enabled = true, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = admin.Id });
                configDb.Add(new VerificationTierSetting { Tier = NrbTier.ADVANCED, Enabled = false, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = admin.Id });
                configDb.SaveChanges();

                Log.Information("Dev seed complete. CDHIB test API key: {Key}", devKey);
            }
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Dev database seeding was skipped or failed.");
    }
}

app.Run();

// Required by xUnit WebApplicationFactory
public partial class Program { }
