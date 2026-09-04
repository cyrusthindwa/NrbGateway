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
[Route("api/v1/portal/cors-origins")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CorsOriginsController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;
    private readonly ICorsOriginManager _corsOriginManager;
    private readonly ILogger<CorsOriginsController> _logger;

    public CorsOriginsController(
        IConfigDbContext configDbContext,
        ICorsOriginManager corsOriginManager,
        ILogger<CorsOriginsController> logger)
    {
        _configDbContext = configDbContext;
        _corsOriginManager = corsOriginManager;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<CorsOriginDto>> GetOrigins()
    {
        var list = _configDbContext.CorsOrigins
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CorsOriginDto(c.Id, c.Origin, c.Description, c.IsEnabled, c.CreatedAt, c.UpdatedAt))
            .ToList();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<CorsOriginDto>> CreateOrigin(
        [FromBody] CreateCorsOriginDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Origin))
        {
            return BadRequest(new { message = "Origin is required." });
        }

        var normalized = dto.Origin.Trim().TrimEnd('/');

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(new { message = "Origin must be a valid absolute URI (e.g. https://app.example.com or http://localhost:3000)." });
        }

        // Clean origin (scheme + host + optional port)
        var cleanedOrigin = $"{uri.Scheme}://{uri.Authority}";

        if (_configDbContext.CorsOrigins.Any(c => c.Origin.ToLower() == cleanedOrigin.ToLower()))
        {
            return Conflict(new { message = $"Origin '{cleanedOrigin}' already exists." });
        }

        var adminId = CurrentAdminId();
        var entry = new CorsOrigin
        {
            Id = Guid.NewGuid(),
            Origin = cleanedOrigin,
            Description = dto.Description?.Trim(),
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _configDbContext.Add(entry);
        _configDbContext.Add(new ConfigAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            SettingArea = SettingArea.CORS_ORIGIN,
            SettingKey = $"cors_origin.{entry.Id}.created",
            OldValue = null,
            NewValue = cleanedOrigin,
            ChangedAt = DateTimeOffset.UtcNow
        });

        await _configDbContext.SaveChangesAsync(cancellationToken);

        // Dynamically allow origin in memory without restarting server
        _corsOriginManager.AddOrEnable(cleanedOrigin);
        _logger.LogInformation("Admin {AdminId} added new CORS origin: {Origin}", adminId, cleanedOrigin);

        return CreatedAtAction(
            nameof(GetOrigins),
            new CorsOriginDto(entry.Id, entry.Origin, entry.Description, entry.IsEnabled, entry.CreatedAt, entry.UpdatedAt));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<CorsOriginDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateCorsOriginDto dto,
        CancellationToken cancellationToken)
    {
        var entry = _configDbContext.CorsOrigins.FirstOrDefault(c => c.Id == id);
        if (entry == null) return NotFound(new { message = "CORS origin not found." });

        var oldStatus = entry.IsEnabled;
        entry.IsEnabled = dto.IsEnabled;
        if (dto.Description != null)
        {
            entry.Description = dto.Description.Trim();
        }
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        _configDbContext.Update(entry);
        _configDbContext.Add(new ConfigAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = CurrentAdminId(),
            SettingArea = SettingArea.CORS_ORIGIN,
            SettingKey = $"cors_origin.{id}.status",
            OldValue = oldStatus.ToString(),
            NewValue = dto.IsEnabled.ToString(),
            ChangedAt = DateTimeOffset.UtcNow
        });

        await _configDbContext.SaveChangesAsync(cancellationToken);

        // Update active CORS manager collection
        if (dto.IsEnabled)
        {
            _corsOriginManager.AddOrEnable(entry.Origin);
        }
        else
        {
            _corsOriginManager.Remove(entry.Origin);
        }

        _logger.LogInformation("CORS origin {Origin} status updated to {IsEnabled}", entry.Origin, dto.IsEnabled);

        return Ok(new CorsOriginDto(entry.Id, entry.Origin, entry.Description, entry.IsEnabled, entry.CreatedAt, entry.UpdatedAt));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOrigin(Guid id, CancellationToken cancellationToken)
    {
        var entry = _configDbContext.CorsOrigins.FirstOrDefault(c => c.Id == id);
        if (entry == null) return NotFound(new { message = "CORS origin not found." });

        _configDbContext.Remove(entry);
        _configDbContext.Add(new ConfigAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = CurrentAdminId(),
            SettingArea = SettingArea.CORS_ORIGIN,
            SettingKey = $"cors_origin.{id}.deleted",
            OldValue = entry.Origin,
            NewValue = null,
            ChangedAt = DateTimeOffset.UtcNow
        });

        await _configDbContext.SaveChangesAsync(cancellationToken);

        // Remove from dynamic CORS manager
        _corsOriginManager.Remove(entry.Origin);
        _logger.LogInformation("CORS origin {Origin} deleted by admin {AdminId}", entry.Origin, CurrentAdminId());

        return NoContent();
    }

    private Guid CurrentAdminId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(claim, out var id) && _configDbContext.AdminUsers.Any(a => a.Id == id))
            return id;

        return _configDbContext.AdminUsers.OrderBy(a => a.CreatedAt).Select(a => a.Id).FirstOrDefault();
    }
}
