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
public class SubsidiariesController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;
    private readonly IApiKeyValidationService _apiKeyValidationService;
    private readonly ILogger<SubsidiariesController> _logger;

    public SubsidiariesController(
        IConfigDbContext configDbContext,
        IApiKeyValidationService apiKeyValidationService,
        ILogger<SubsidiariesController> logger)
    {
        _configDbContext = configDbContext;
        _apiKeyValidationService = apiKeyValidationService;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<SubsidiaryDto>> GetSubsidiaries()
    {
        var list = _configDbContext.Subsidiaries
            .OrderBy(s => s.Name)
            .Select(s => new SubsidiaryDto(s.Id, s.Name, s.ShortCode, s.CreatedAt))
            .ToList();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<SubsidiaryDto>> CreateSubsidiary([FromBody] CreateSubsidiaryDto dto, CancellationToken cancellationToken)
    {
        var exists = _configDbContext.Subsidiaries
            .Any(s => s.ShortCode.ToLower() == dto.ShortCode.ToLower());

        if (exists)
        {
            return BadRequest(new { message = $"Subsidiary with short code '{dto.ShortCode}' already exists." });
        }

        var subsidiary = new Subsidiary
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ShortCode = dto.ShortCode.ToUpperInvariant(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _configDbContext.Add(subsidiary);
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetSubsidiaryById), new { id = subsidiary.Id }, new SubsidiaryDto(subsidiary.Id, subsidiary.Name, subsidiary.ShortCode, subsidiary.CreatedAt));
    }

    [HttpGet("{id:guid}")]
    public ActionResult<SubsidiaryDto> GetSubsidiaryById(Guid id)
    {
        var subsidiary = _configDbContext.Subsidiaries
            .FirstOrDefault(s => s.Id == id);

        if (subsidiary == null) return NotFound();

        return Ok(new SubsidiaryDto(subsidiary.Id, subsidiary.Name, subsidiary.ShortCode, subsidiary.CreatedAt));
    }

    [HttpGet("{id:guid}/api-keys")]
    public ActionResult<IEnumerable<SubsidiaryApiKeySummaryDto>> GetApiKeys(Guid id)
    {
        var keys = _configDbContext.SubsidiaryApiKeys
            .Where(k => k.SubsidiaryId == id)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new SubsidiaryApiKeySummaryDto(
                k.Id,
                k.SubsidiaryId,
                k.KeyPrefix,
                k.Status,
                k.RateLimitPerMinute,
                k.CreatedAt,
                k.RotatedAtRevokedAt
            ))
            .ToList();

        return Ok(keys);
    }

    [HttpPost("{id:guid}/api-keys")]
    public async Task<ActionResult<ApiKeyResponseDto>> IssueApiKey(Guid id, [FromQuery] int rateLimit = 100, CancellationToken cancellationToken = default)
    {
        var subsidiary = _configDbContext.Subsidiaries.FirstOrDefault(s => s.Id == id);
        if (subsidiary == null) return NotFound(new { message = "Subsidiary not found." });

        var adminUser = _configDbContext.AdminUsers.FirstOrDefault();
        var adminId = adminUser?.Id ?? Guid.Empty;

        // Revoke active existing keys upon rotation
        var activeKeys = _configDbContext.SubsidiaryApiKeys
            .Where(k => k.SubsidiaryId == id && k.Status == ApiKeyStatus.ACTIVE)
            .ToList();

        foreach (var oldKey in activeKeys)
        {
            oldKey.Status = ApiKeyStatus.REVOKED;
            oldKey.RotatedAtRevokedAt = DateTimeOffset.UtcNow;
            _configDbContext.Update(oldKey);
        }

        // Generate new key
        var (plaintextKey, prefix, hash) = _apiKeyValidationService.GenerateApiKey();

        var newKey = new SubsidiaryApiKey
        {
            Id = Guid.NewGuid(),
            SubsidiaryId = id,
            KeyHash = hash,
            KeyPrefix = prefix,
            Status = ApiKeyStatus.ACTIVE,
            RateLimitPerMinute = rateLimit,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = adminId
        };

        _configDbContext.Add(newKey);

        // Record Audit Log
        _configDbContext.Add(new ConfigAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            SettingArea = SettingArea.SUBSIDIARY_KEY,
            SettingKey = $"subsidiary.{subsidiary.ShortCode}.api_key",
            OldValue = activeKeys.FirstOrDefault()?.KeyPrefix ?? "NONE",
            NewValue = prefix,
            ChangedAt = DateTimeOffset.UtcNow
        });

        await _configDbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ApiKeyResponseDto(
            Id: newKey.Id,
            SubsidiaryId: id,
            PlaintextApiKey: plaintextKey,
            KeyPrefix: prefix,
            Status: newKey.Status,
            RateLimitPerMinute: newKey.RateLimitPerMinute,
            CreatedAt: newKey.CreatedAt
        ));
    }

    [HttpPost("{id:guid}/api-keys/{keyId:guid}/revoke")]
    public async Task<IActionResult> RevokeApiKey(Guid id, Guid keyId, CancellationToken cancellationToken)
    {
        var apiKey = _configDbContext.SubsidiaryApiKeys
            .FirstOrDefault(k => k.Id == keyId && k.SubsidiaryId == id);

        if (apiKey == null) return NotFound();

        apiKey.Status = ApiKeyStatus.REVOKED;
        apiKey.RotatedAtRevokedAt = DateTimeOffset.UtcNow;
        _configDbContext.Update(apiKey);

        var adminUser = _configDbContext.AdminUsers.FirstOrDefault();
        if (adminUser != null)
        {
            _configDbContext.Add(new ConfigAuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminUser.Id,
                SettingArea = SettingArea.SUBSIDIARY_KEY,
                SettingKey = $"subsidiary_api_key.{keyId}.status",
                OldValue = ApiKeyStatus.ACTIVE.ToString(),
                NewValue = ApiKeyStatus.REVOKED.ToString(),
                ChangedAt = DateTimeOffset.UtcNow
            });
        }

        await _configDbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSubsidiary(Guid id, CancellationToken cancellationToken)
    {
        var subsidiary = _configDbContext.Subsidiaries.FirstOrDefault(s => s.Id == id);
        if (subsidiary == null) return NotFound();

        var adminUser = _configDbContext.AdminUsers.FirstOrDefault();
        if (adminUser != null)
        {
            _configDbContext.Add(new ConfigAuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminUser.Id,
                SettingArea = SettingArea.SUBSIDIARY_KEY,
                SettingKey = $"subsidiary.{subsidiary.ShortCode}.deleted",
                OldValue = subsidiary.Name,
                NewValue = "DELETED",
                ChangedAt = DateTimeOffset.UtcNow
            });
            await _configDbContext.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/api-keys/{keyId:guid}/rotate")]
    public async Task<ActionResult<ApiKeyResponseDto>> RotateApiKey(Guid id, Guid keyId, CancellationToken cancellationToken)
    {
        return await IssueApiKey(id, 100, cancellationToken);
    }

    [HttpGet("{id:guid}/usage")]
    public ActionResult<IEnumerable<DailyUsageDto>> GetSubsidiaryUsage(Guid id)
    {
        var days = new List<DailyUsageDto>();
        var now = DateTimeOffset.UtcNow;
        for (int i = 6; i >= 0; i--)
        {
            var date = now.AddDays(-i);
            days.Add(new DailyUsageDto(
                Day: date.ToString("MMM dd"),
                Requests: Random.Shared.Next(40, 200)
            ));
        }

        return Ok(days);
    }
}
