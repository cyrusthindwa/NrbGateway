using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
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
}
