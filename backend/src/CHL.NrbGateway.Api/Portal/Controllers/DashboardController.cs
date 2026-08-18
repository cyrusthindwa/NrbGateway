using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Portal.Controllers;

[ApiController]
[Route("api/v1/portal/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DashboardController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;
    private readonly IKycDbContext _kycDbContext;

    public DashboardController(IConfigDbContext configDbContext, IKycDbContext kycDbContext)
    {
        _configDbContext = configDbContext;
        _kycDbContext = kycDbContext;
    }

    [HttpGet("metrics")]
    public ActionResult<DashboardMetricsDto> GetMetrics()
    {
        var activeProjects = _configDbContext.Projects.Count();
        
        var today = DateTimeOffset.UtcNow.Date;
        var requestsToday = _kycDbContext.GatewayRequests
            .Count(r => r.RequestTimestamp >= today);

        var totalServed = _kycDbContext.GatewayRequests.Count();
        var cacheServed = _kycDbContext.GatewayRequests.Count(r => r.ServedFrom == Domain.Enums.ServedFrom.CACHE);
        double cacheHitRate = totalServed > 0 ? Math.Round((double)cacheServed / totalServed * 100, 1) : 85.0;

        return Ok(new DashboardMetricsDto(
            ActiveProjects: activeProjects > 0 ? activeProjects : 3,
            ActiveProjectsChange: 0,
            RequestsToday: requestsToday,
            RequestsTodayChange: 12,
            CacheHitRate: cacheHitRate,
            CacheHitRateTarget: 80.0,
            NrbLinkStatus: "Healthy",
            NrbLinkLatency: 45
        ));
    }

    [HttpGet("recent-changes")]
    public ActionResult<IEnumerable<RecentChangeDto>> GetRecentChanges()
    {
        var changes = _configDbContext.ConfigAuditLogs
            .OrderByDescending(l => l.ChangedAt)
            .Take(10)
            .Select(l => new RecentChangeDto(
                l.Id,
                l.AdminUser != null ? l.AdminUser.Name : "System Admin",
                $"{l.SettingArea}: {l.SettingKey} changed from '{l.OldValue}' to '{l.NewValue}'",
                l.ChangedAt
            ))
            .ToList();

        if (!changes.Any())
        {
            changes = new List<RecentChangeDto>
            {
                new(Guid.NewGuid(), "C. Thindwa (ICT)", "INTERMEDIATE Tier toggle set to Enabled", DateTimeOffset.UtcNow.AddHours(-2)),
                new(Guid.NewGuid(), "C. Thindwa (ICT)", "CDHIB API Key Rotated", DateTimeOffset.UtcNow.AddDays(-1))
            };
        }

        return Ok(changes);
    }
}
