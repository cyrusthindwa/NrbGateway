using System.Security.Claims;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Portal.Controllers;

[ApiController]
[Route("api/v1/portal/audit-log")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AuditLogController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;

    public AuditLogController(IConfigDbContext configDbContext)
    {
        _configDbContext = configDbContext;
    }

    [HttpGet]
    public ActionResult<PaginatedResponseDto<AuditLogEntryDto>> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? admin = null,
        [FromQuery] string? actionType = null)
    {
        var logsQuery = _configDbContext.ConfigAuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(admin) && Guid.TryParse(admin, out var adminId))
            logsQuery = logsQuery.Where(l => l.AdminId == adminId);

        if (!string.IsNullOrWhiteSpace(actionType) && Enum.TryParse<SettingArea>(actionType, out var area))
            logsQuery = logsQuery.Where(l => l.SettingArea == area);

        var total = logsQuery.Count();

        var data = logsQuery
            .OrderByDescending(l => l.ChangedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogEntryDto(
                l.Id,
                l.ChangedAt,
                l.AdminUser != null ? l.AdminUser.Name : "System Admin",
                l.SettingKey,
                l.OldValue ?? "-",
                l.NewValue ?? "-",
                l.SettingArea.ToString()
            ))
            .ToList();

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        if (totalPages < 1) totalPages = 1;

        return Ok(new PaginatedResponseDto<AuditLogEntryDto>(
            Data: data,
            Total: total,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages
        ));
    }

    [HttpPost("{id:guid}/rollback")]
    public async Task<IActionResult> Rollback(Guid id, CancellationToken cancellationToken)
    {
        var entry = _configDbContext.ConfigAuditLogs.FirstOrDefault(l => l.Id == id);
        if (entry == null) return NotFound(new { message = "Audit entry not found." });

        var adminId = CurrentAdminId();

        switch (entry.SettingArea)
        {
            case SettingArea.TIER_TOGGLE:
                var tierParts = entry.SettingKey.Split('.');
                if (tierParts.Length != 3 || tierParts[0] != "tier" || tierParts[2] != "enabled")
                    return BadRequest(new { message = "This tier change cannot be rolled back." });
                if (!Enum.TryParse<NrbTier>(tierParts[1], out var tier))
                    return BadRequest(new { message = "Cannot resolve tier for rollback." });
                if (!bool.TryParse(entry.OldValue, out var enabled))
                    return BadRequest(new { message = "Cannot resolve previous value." });
                var tierSetting = _configDbContext.VerificationTierSettings.FirstOrDefault(t => t.Tier == tier);
                if (tierSetting == null) return BadRequest(new { message = "Tier setting no longer exists." });
                tierSetting.Enabled = enabled;
                tierSetting.UpdatedAt = DateTimeOffset.UtcNow;
                tierSetting.UpdatedBy = adminId;
                _configDbContext.Update(tierSetting);
                break;

            case SettingArea.RATE_LIMIT:
                var rateParts = entry.SettingKey.Split('.');
                if (rateParts.Length != 3 || rateParts[0] != "project_api_key" || rateParts[2] != "rate_limit")
                    return BadRequest(new { message = "This rate-limit change cannot be rolled back." });
                if (!Guid.TryParse(rateParts[1], out var keyId))
                    return BadRequest(new { message = "Cannot resolve API key for rollback." });
                if (!int.TryParse(entry.OldValue, out var oldRate))
                    return BadRequest(new { message = "Cannot resolve previous value." });
                var apiKey = _configDbContext.ProjectApiKeys.FirstOrDefault(k => k.Id == keyId);
                if (apiKey == null) return BadRequest(new { message = "API key no longer exists." });
                apiKey.RateLimitPerMinute = oldRate;
                _configDbContext.Update(apiKey);
                break;

            case SettingArea.NRB_ENVIRONMENT:
                if (entry.SettingKey != "nrb_environment.active")
                    return BadRequest(new { message = "This environment change cannot be rolled back." });
                if (!Enum.TryParse<NrbEnvironment>(entry.OldValue, out var environment))
                    return BadRequest(new { message = "Cannot resolve previous environment." });
                var envSetting = _configDbContext.NrbEnvironmentSettings.OrderByDescending(e => e.UpdatedAt).FirstOrDefault();
                if (envSetting == null) return BadRequest(new { message = "Environment setting no longer exists." });
                envSetting.Environment = environment;
                envSetting.UpdatedAt = DateTimeOffset.UtcNow;
                envSetting.UpdatedBy = adminId;
                _configDbContext.Update(envSetting);
                break;

            default:
                return BadRequest(new { message = "This change type cannot be rolled back." });
        }

        _configDbContext.Add(new ConfigAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            SettingArea = entry.SettingArea,
            SettingKey = $"{entry.SettingKey}.rollback",
            OldValue = entry.NewValue,
            NewValue = entry.OldValue,
            ChangedAt = DateTimeOffset.UtcNow,
            RollbackOfId = entry.Id
        });
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Change has been rolled back." });
    }

    private Guid CurrentAdminId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(claim, out var id) && _configDbContext.AdminUsers.Any(a => a.Id == id))
            return id;

        return _configDbContext.AdminUsers.OrderBy(a => a.CreatedAt).Select(a => a.Id).FirstOrDefault();
    }
}
