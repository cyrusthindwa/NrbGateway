using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Portal.Controllers;

[ApiController]
[Route("api/v1/portal/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SettingsController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        IConfigDbContext configDbContext,
        ILogger<SettingsController> logger)
    {
        _configDbContext = configDbContext;
        _logger = logger;
    }

    [HttpGet("tiers")]
    public async Task<ActionResult<IEnumerable<TierSettingDto>>> GetTierSettings(CancellationToken cancellationToken)
    {
        var settings = _configDbContext.VerificationTierSettings
            .Select(t => new TierSettingDto(t.Tier, t.Enabled, t.CostPerRequest, t.UpdatedAt, t.UpdatedBy))
            .ToList();

        // Ensure default records for all 4 tiers exist if empty
        if (!settings.Any())
        {
            var adminUser = _configDbContext.AdminUsers.FirstOrDefault();
            var adminId = adminUser?.Id ?? Guid.Empty;

            var defaultTiers = new[]
            {
                new VerificationTierSetting { Tier = NrbTier.BASIC, Enabled = false, CostPerRequest = 0m, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = adminId },
                new VerificationTierSetting { Tier = NrbTier.TEXT_LOOKUP, Enabled = false, CostPerRequest = 0m, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = adminId },
                new VerificationTierSetting { Tier = NrbTier.INTERMEDIATE, Enabled = true, CostPerRequest = 0m, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = adminId }, // MVP active
                new VerificationTierSetting { Tier = NrbTier.ADVANCED, Enabled = false, CostPerRequest = 0m, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = adminId }
            };

            foreach (var tier in defaultTiers)
            {
                _configDbContext.Add(tier);
            }
            await _configDbContext.SaveChangesAsync(cancellationToken);

            settings = defaultTiers.Select(t => new TierSettingDto(t.Tier, t.Enabled, t.CostPerRequest, t.UpdatedAt, t.UpdatedBy)).ToList();
        }

        return Ok(settings);
    }

    [HttpPut("tiers/{tier}")]
    public async Task<ActionResult<TierSettingDto>> UpdateTierSetting(NrbTier tier, [FromBody] UpdateTierSettingDto dto, CancellationToken cancellationToken)
    {
        var adminUser = _configDbContext.AdminUsers.FirstOrDefault();
        var adminId = adminUser?.Id ?? Guid.Empty;

        var setting = _configDbContext.VerificationTierSettings
            .FirstOrDefault(t => t.Tier == tier);

        if (setting == null)
        {
            setting = new VerificationTierSetting
            {
                Tier = tier,
                Enabled = dto.Enabled,
                CostPerRequest = dto.CostPerRequest ?? 0m,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = adminId
            };
            _configDbContext.Add(setting);
        }
        else
        {
            var oldEnabled = setting.Enabled;
            setting.Enabled = dto.Enabled;
            setting.CostPerRequest = dto.CostPerRequest ?? setting.CostPerRequest;
            setting.UpdatedAt = DateTimeOffset.UtcNow;
            setting.UpdatedBy = adminId;
            _configDbContext.Update(setting);

            _configDbContext.Add(new ConfigAuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                SettingArea = SettingArea.TIER_TOGGLE,
                SettingKey = $"tier.{tier}.enabled",
                OldValue = oldEnabled.ToString(),
                NewValue = dto.Enabled.ToString(),
                ChangedAt = DateTimeOffset.UtcNow
            });
        }

        await _configDbContext.SaveChangesAsync(cancellationToken);

        return Ok(new TierSettingDto(setting.Tier, setting.Enabled, setting.CostPerRequest, setting.UpdatedAt, setting.UpdatedBy));
    }

    [HttpGet("nrb-environment")]
    public async Task<ActionResult<EnvironmentSettingDto>> GetEnvironmentSettings(CancellationToken cancellationToken)
    {
        var envSetting = _configDbContext.NrbEnvironmentSettings
            .OrderByDescending(e => e.UpdatedAt)
            .FirstOrDefault();

        if (envSetting == null)
        {
            var adminUser = _configDbContext.AdminUsers.FirstOrDefault();
            var adminId = adminUser?.Id ?? Guid.Empty;

            envSetting = new NrbEnvironmentSetting
            {
                Id = Guid.NewGuid(),
                Environment = NrbEnvironment.TEST,
                BasicEndpointUrl = "https://nrb-api-test.cict.gov.mw/verify/postverify",
                TextLookupEndpointUrl = "https://nrb-api-test.cict.gov.mw/api/person",
                IntermediateEndpointUrl = "https://nrb-api-test.cict.gov.mw/middleware/iVerify",
                AdvancedEndpointUrl = "https://nrb-api-test.cict.gov.mw/middleware/aVerify",
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = adminId
            };

            _configDbContext.Add(envSetting);
            await _configDbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new EnvironmentSettingDto(
            envSetting.Id,
            envSetting.Environment,
            envSetting.BasicEndpointUrl,
            envSetting.TextLookupEndpointUrl,
            envSetting.IntermediateEndpointUrl,
            envSetting.AdvancedEndpointUrl,
            envSetting.UpdatedAt,
            envSetting.UpdatedBy
        ));
    }

    [HttpPut("nrb-environment")]
    public async Task<ActionResult<EnvironmentSettingDto>> UpdateEnvironmentSettings([FromBody] UpdateEnvironmentSettingDto dto, CancellationToken cancellationToken)
    {
        var adminUser = _configDbContext.AdminUsers.FirstOrDefault();
        var adminId = adminUser?.Id ?? Guid.Empty;

        var envSetting = _configDbContext.NrbEnvironmentSettings
            .OrderByDescending(e => e.UpdatedAt)
            .FirstOrDefault();

        if (envSetting == null)
        {
            envSetting = new NrbEnvironmentSetting
            {
                Id = Guid.NewGuid(),
                Environment = dto.Environment,
                BasicEndpointUrl = dto.BasicEndpointUrl ?? "https://nrb-api-test.cict.gov.mw/verify/postverify",
                TextLookupEndpointUrl = dto.TextLookupEndpointUrl ?? "https://nrb-api-test.cict.gov.mw/api/person",
                IntermediateEndpointUrl = dto.IntermediateEndpointUrl ?? "https://nrb-api-test.cict.gov.mw/middleware/iVerify",
                AdvancedEndpointUrl = dto.AdvancedEndpointUrl ?? "https://nrb-api-test.cict.gov.mw/middleware/aVerify",
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = adminId
            };
            _configDbContext.Add(envSetting);
        }
        else
        {
            var oldEnv = envSetting.Environment.ToString();
            envSetting.Environment = dto.Environment;
            if (dto.BasicEndpointUrl != null) envSetting.BasicEndpointUrl = dto.BasicEndpointUrl;
            if (dto.TextLookupEndpointUrl != null) envSetting.TextLookupEndpointUrl = dto.TextLookupEndpointUrl;
            if (dto.IntermediateEndpointUrl != null) envSetting.IntermediateEndpointUrl = dto.IntermediateEndpointUrl;
            if (dto.AdvancedEndpointUrl != null) envSetting.AdvancedEndpointUrl = dto.AdvancedEndpointUrl;
            envSetting.UpdatedAt = DateTimeOffset.UtcNow;
            envSetting.UpdatedBy = adminId;
            _configDbContext.Update(envSetting);

            _configDbContext.Add(new ConfigAuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                SettingArea = SettingArea.NRB_ENVIRONMENT,
                SettingKey = "nrb_environment.active",
                OldValue = oldEnv,
                NewValue = dto.Environment.ToString(),
                ChangedAt = DateTimeOffset.UtcNow
            });
        }

        await _configDbContext.SaveChangesAsync(cancellationToken);

        return Ok(new EnvironmentSettingDto(
            envSetting.Id,
            envSetting.Environment,
            envSetting.BasicEndpointUrl,
            envSetting.TextLookupEndpointUrl,
            envSetting.IntermediateEndpointUrl,
            envSetting.AdvancedEndpointUrl,
            envSetting.UpdatedAt,
            envSetting.UpdatedBy
        ));
    }
}
