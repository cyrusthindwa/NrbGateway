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
[Route("api/v1/portal/notification-channels")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationChannelsController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;

    public NotificationChannelsController(IConfigDbContext configDbContext)
    {
        _configDbContext = configDbContext;
    }

    [HttpGet]
    public ActionResult<IEnumerable<NotificationChannelDto>> GetChannels()
    {
        var list = _configDbContext.NotificationChannels
            .OrderBy(c => c.ChannelType)
            .ThenBy(c => c.Target)
            .Select(c => new NotificationChannelDto(c.Id, c.ChannelType, c.Target, c.Enabled, c.CreatedBy, c.CreatedAt))
            .ToList();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<NotificationChannelDto>> CreateChannel(
        [FromBody] CreateNotificationChannelDto dto,
        CancellationToken cancellationToken)
    {
        var adminId = CurrentAdminId();

        var channel = new NotificationChannel
        {
            Id = Guid.NewGuid(),
            ChannelType = dto.ChannelType,
            Target = dto.Target.Trim(),
            Enabled = true,
            CreatedBy = adminId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _configDbContext.Add(channel);
        _configDbContext.Add(new ConfigAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            SettingArea = SettingArea.NOTIFICATION_CHANNEL,
            SettingKey = $"notification_channel.{channel.Id}.created",
            OldValue = null,
            NewValue = $"{channel.ChannelType}:{channel.Target}",
            ChangedAt = DateTimeOffset.UtcNow
        });
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetChannels),
            new NotificationChannelDto(channel.Id, channel.ChannelType, channel.Target, channel.Enabled, channel.CreatedBy, channel.CreatedAt));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<NotificationChannelDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateNotificationChannelDto dto,
        CancellationToken cancellationToken)
    {
        var channel = _configDbContext.NotificationChannels.FirstOrDefault(c => c.Id == id);
        if (channel == null) return NotFound(new { message = "Notification channel not found." });

        channel.Enabled = dto.Enabled;
        _configDbContext.Update(channel);

        _configDbContext.Add(new ConfigAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = CurrentAdminId(),
            SettingArea = SettingArea.NOTIFICATION_CHANNEL,
            SettingKey = $"notification_channel.{id}.enabled",
            OldValue = (!dto.Enabled).ToString(),
            NewValue = dto.Enabled.ToString(),
            ChangedAt = DateTimeOffset.UtcNow
        });
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return Ok(new NotificationChannelDto(channel.Id, channel.ChannelType, channel.Target, channel.Enabled, channel.CreatedBy, channel.CreatedAt));
    }

    private Guid CurrentAdminId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(claim, out var id) && _configDbContext.AdminUsers.Any(a => a.Id == id))
            return id;

        return _configDbContext.AdminUsers.OrderBy(a => a.CreatedAt).Select(a => a.Id).FirstOrDefault();
    }
}
