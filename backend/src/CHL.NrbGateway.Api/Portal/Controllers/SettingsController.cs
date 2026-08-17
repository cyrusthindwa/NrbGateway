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
            .Select(t => new TierSettingDto(t.Tier, t.Enabled, t.UpdatedAt, t.UpdatedBy))
            .ToList();

        // Ensure default records for all 4 tiers exist if empty
        if (!settings.Any())
        {
            var adminUser = _configDbContext.AdminUsers.FirstOrDefault();
            var adminId = adminUser?.Id ?? Guid.Empty;

            var defaultTiers = new[]
            {
                new VerificationTierSetting { Tier = NrbTier.BASIC, Enabled = false, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = adminId },
                new VerificationTierSetting { Tier = NrbTier.TEXT_LOOKUP, Enabled = false, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = adminId },
                new VerificationTierSetting { Tier = NrbTier.INTERMEDIATE, Enabled = true, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = adminId }, // MVP active
                new VerificationTierSetting { Tier = NrbTier.ADVANCED, Enabled = false, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = adminId }
            };

            foreach (var tier in defaultTiers)
            {
                _configDbContext.Add(tier);
            }
            await _configDbContext.SaveChangesAsync(cancellationToken);

            settings = defaultTiers.Select(t => new TierSettingDto(t.Tier, t.Enabled, t.UpdatedAt, t.UpdatedBy)).ToList();
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
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = adminId
            };
            _configDbContext.Add(setting);
        }
        else
        {
            var oldEnabled = setting.Enabled;
            setting.Enabled = dto.Enabled;
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

        return Ok(new TierSettingDto(setting.Tier, setting.Enabled, setting.UpdatedAt, setting.UpdatedBy));
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
                BasicEndpointUrl = dto.BasicEndpointUrl,
                TextLookupEndpointUrl = dto.TextLookupEndpointUrl,
                IntermediateEndpointUrl = dto.IntermediateEndpointUrl,
                AdvancedEndpointUrl = dto.AdvancedEndpointUrl,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = adminId
            };
            _configDbContext.Add(envSetting);
        }
        else
        {
            var oldEnv = envSetting.Environment.ToString();
            envSetting.Environment = dto.Environment;
            envSetting.BasicEndpointUrl = dto.BasicEndpointUrl;
            envSetting.TextLookupEndpointUrl = dto.TextLookupEndpointUrl;
            envSetting.IntermediateEndpointUrl = dto.IntermediateEndpointUrl;
            envSetting.AdvancedEndpointUrl = dto.AdvancedEndpointUrl;
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

    [HttpGet("cache-policy")]
    public ActionResult<CachePolicyDto> GetCachePolicy()
    {
        var bioPolicy = _configDbContext.CacheRetentionPolicies
            .FirstOrDefault(c => c.DataType == DataType.BIOGRAPHIC_RECORD);
        var eventPolicy = _configDbContext.CacheRetentionPolicies
            .FirstOrDefault(c => c.DataType == DataType.VERIFICATION_EVENT);

        return Ok(new CachePolicyDto(
            BiographicRecordFreshness: bioPolicy?.FreshnessValue ?? 30,
            BiographicRecordFreshnessUnit: bioPolicy?.FreshnessUnit.ToString() ?? "DAYS",
            VerificationEventFreshness: eventPolicy?.FreshnessValue ?? 24,
            VerificationEventFreshnessUnit: eventPolicy?.FreshnessUnit.ToString() ?? "HOURS",
            AuditLogRetentionDays: 90
        ));
    }

    [HttpPut("cache-policy")]
    public async Task<ActionResult<CachePolicyDto>> UpdateCachePolicy([FromBody] CachePolicyDto dto, CancellationToken cancellationToken)
    {
        var adminUser = _configDbContext.AdminUsers.FirstOrDefault();
        var adminId = adminUser?.Id ?? Guid.Empty;

        var bioPolicy = _configDbContext.CacheRetentionPolicies
            .FirstOrDefault(c => c.DataType == DataType.BIOGRAPHIC_RECORD);
        if (bioPolicy == null)
        {
            bioPolicy = new CacheRetentionPolicy
            {
                Id = Guid.NewGuid(),
                DataType = DataType.BIOGRAPHIC_RECORD,
                FreshnessValue = dto.BiographicRecordFreshness,
                FreshnessUnit = Enum.TryParse<FreshnessUnit>(dto.BiographicRecordFreshnessUnit, true, out var u1) ? u1 : FreshnessUnit.DAYS,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = adminId
            };
            _configDbContext.Add(bioPolicy);
        }
        else
        {
            bioPolicy.FreshnessValue = dto.BiographicRecordFreshness;
            bioPolicy.FreshnessUnit = Enum.TryParse<FreshnessUnit>(dto.BiographicRecordFreshnessUnit, true, out var u1) ? u1 : FreshnessUnit.DAYS;
            bioPolicy.UpdatedAt = DateTimeOffset.UtcNow;
            bioPolicy.UpdatedBy = adminId;
            _configDbContext.Update(bioPolicy);
        }

        var eventPolicy = _configDbContext.CacheRetentionPolicies
            .FirstOrDefault(c => c.DataType == DataType.VERIFICATION_EVENT);
        if (eventPolicy == null)
        {
            eventPolicy = new CacheRetentionPolicy
            {
                Id = Guid.NewGuid(),
                DataType = DataType.VERIFICATION_EVENT,
                FreshnessValue = dto.VerificationEventFreshness,
                FreshnessUnit = Enum.TryParse<FreshnessUnit>(dto.VerificationEventFreshnessUnit, true, out var u2) ? u2 : FreshnessUnit.HOURS,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = adminId
            };
            _configDbContext.Add(eventPolicy);
        }
        else
        {
            eventPolicy.FreshnessValue = dto.VerificationEventFreshness;
            eventPolicy.FreshnessUnit = Enum.TryParse<FreshnessUnit>(dto.VerificationEventFreshnessUnit, true, out var u2) ? u2 : FreshnessUnit.HOURS;
            eventPolicy.UpdatedAt = DateTimeOffset.UtcNow;
            eventPolicy.UpdatedBy = adminId;
            _configDbContext.Update(eventPolicy);
        }

        await _configDbContext.SaveChangesAsync(cancellationToken);

        return Ok(dto);
    }
}
