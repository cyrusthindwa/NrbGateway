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
public class ProjectsController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;
    private readonly IApiKeyValidationService _apiKeyValidationService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IConfigDbContext configDbContext,
        IApiKeyValidationService apiKeyValidationService,
        ILogger<ProjectsController> logger)
    {
        _configDbContext = configDbContext;
        _apiKeyValidationService = apiKeyValidationService;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ProjectDto>> GetProjects()
    {
        var list = _configDbContext.Projects
            .OrderBy(p => p.Name)
            .Select(p => new ProjectDto(p.Id, p.CompanyId, p.Name, p.ShortCode, p.CreatedAt))
            .ToList();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectDto dto, CancellationToken cancellationToken)
    {
        var company = _configDbContext.Companies.FirstOrDefault(c => c.Id == dto.CompanyId);
        if (company == null)
            return BadRequest(new { message = $"Company '{dto.CompanyId}' does not exist." });

        var exists = _configDbContext.Projects
            .Any(p => p.ShortCode.ToLower() == dto.ShortCode.ToLower());

        if (exists)
            return BadRequest(new { message = $"Project with short code '{dto.ShortCode}' already exists." });

        var project = new Project
        {
            Id = Guid.NewGuid(),
            CompanyId = dto.CompanyId,
            Name = dto.Name,
            ShortCode = dto.ShortCode.ToUpperInvariant(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _configDbContext.Add(project);
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetProjectById), new { id = project.Id },
            new ProjectDto(project.Id, project.CompanyId, project.Name, project.ShortCode, project.CreatedAt));
    }

    [HttpGet("{id:guid}")]
    public ActionResult<ProjectDto> GetProjectById(Guid id)
    {
        var project = _configDbContext.Projects.FirstOrDefault(p => p.Id == id);
        if (project == null) return NotFound();

        return Ok(new ProjectDto(project.Id, project.CompanyId, project.Name, project.ShortCode, project.CreatedAt));
    }

    [HttpGet("{id:guid}/api-keys")]
    public ActionResult<IEnumerable<ProjectApiKeySummaryDto>> GetApiKeys(Guid id)
    {
        var keys = _configDbContext.ProjectApiKeys
            .Where(k => k.ProjectId == id)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ProjectApiKeySummaryDto(
                k.Id,
                k.ProjectId,
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
    public async Task<ActionResult<ApiKeyResponseDto>> IssueApiKey(
        Guid id,
        [FromQuery] int rateLimit = 100,
        [FromQuery] ApiKeyEnvironment environment = ApiKeyEnvironment.TEST,
        CancellationToken cancellationToken = default)
    {
        var project = _configDbContext.Projects.FirstOrDefault(p => p.Id == id);
        if (project == null) return NotFound(new { message = "Project not found." });

        var adminUser = _configDbContext.AdminUsers.FirstOrDefault();
        var adminId = adminUser?.Id ?? Guid.Empty;

        // Revoke active existing keys upon rotation
        var activeKeys = _configDbContext.ProjectApiKeys
            .Where(k => k.ProjectId == id && k.Status == ApiKeyStatus.ACTIVE)
            .ToList();

        foreach (var oldKey in activeKeys)
        {
            oldKey.Status = ApiKeyStatus.REVOKED;
            oldKey.RotatedAtRevokedAt = DateTimeOffset.UtcNow;
            _configDbContext.Update(oldKey);
        }

        // Generate new key
        var (plaintextKey, prefix, hash) = _apiKeyValidationService.GenerateApiKey(environment);

        var newKey = new ProjectApiKey
        {
            Id = Guid.NewGuid(),
            ProjectId = id,
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
            SettingArea = SettingArea.PROJECT_KEY,
            SettingKey = $"project.{project.ShortCode}.api_key",
            OldValue = activeKeys.FirstOrDefault()?.KeyPrefix ?? "NONE",
            NewValue = prefix,
            ChangedAt = DateTimeOffset.UtcNow
        });

        await _configDbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ApiKeyResponseDto(
            Id: newKey.Id,
            ProjectId: id,
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
        var apiKey = _configDbContext.ProjectApiKeys
            .FirstOrDefault(k => k.Id == keyId && k.ProjectId == id);

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
                SettingArea = SettingArea.PROJECT_KEY,
                SettingKey = $"project_api_key.{keyId}.status",
                OldValue = ApiKeyStatus.ACTIVE.ToString(),
                NewValue = ApiKeyStatus.REVOKED.ToString(),
                ChangedAt = DateTimeOffset.UtcNow
            });
        }

        await _configDbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProject(Guid id, CancellationToken cancellationToken)
    {
        var project = _configDbContext.Projects.FirstOrDefault(p => p.Id == id);
        if (project == null) return NotFound();

        var adminUser = _configDbContext.AdminUsers.FirstOrDefault();
        if (adminUser != null)
        {
            _configDbContext.Add(new ConfigAuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = adminUser.Id,
                SettingArea = SettingArea.PROJECT_KEY,
                SettingKey = $"project.{project.ShortCode}.deleted",
                OldValue = project.Name,
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
        return await IssueApiKey(id, 100, ApiKeyEnvironment.TEST, cancellationToken);
    }

    [HttpGet("{id:guid}/usage")]
    public ActionResult<IEnumerable<DailyUsageDto>> GetProjectUsage(Guid id)
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
