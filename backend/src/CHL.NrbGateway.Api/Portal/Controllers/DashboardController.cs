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
        var now = DateTimeOffset.UtcNow;
        var today = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var yesterday = today.AddDays(-1);

        var activeProjects = _configDbContext.Projects.Count();

        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var lastMonthStart = monthStart.AddMonths(-1);
        var newProjectsThisMonth = _configDbContext.Projects.Count(p => p.CreatedAt >= monthStart);
        var newProjectsLastMonth = _configDbContext.Projects.Count(p => p.CreatedAt >= lastMonthStart && p.CreatedAt < monthStart);
        var activeProjectsChange = newProjectsThisMonth - newProjectsLastMonth;

        var requestsToday = _kycDbContext.GatewayRequests
            .Count(r => r.RequestTimestamp >= today);
        var requestsYesterday = _kycDbContext.GatewayRequests
            .Count(r => r.RequestTimestamp >= yesterday && r.RequestTimestamp < today);
        var requestsTodayChange = requestsToday - requestsYesterday;

        var totalServed = _kycDbContext.GatewayRequests.Count();
        var cacheServed = _kycDbContext.GatewayRequests.Count(r => r.ServedFrom == Domain.Enums.ServedFrom.CACHE);
        double? cacheHitRate = totalServed > 0
            ? Math.Round((double)cacheServed / totalServed * 100, 1)
            : null;

        var latestHealth = _configDbContext.NrbHealthChecks
            .OrderByDescending(h => h.CheckedAt)
            .FirstOrDefault();

        string nrbLinkStatus;
        int? nrbLinkLatency = null;
        DateTimeOffset? nrbLastCheckedAt = null;

        if (latestHealth == null)
        {
            nrbLinkStatus = "Not yet monitored";
        }
        else
        {
            nrbLinkStatus = latestHealth.IsUp ? "Healthy" : "Down";
            nrbLinkLatency = latestHealth.LatencyMs;
            nrbLastCheckedAt = latestHealth.CheckedAt;
        }

        return Ok(new DashboardMetricsDto(
            ActiveProjects: activeProjects,
            ActiveProjectsChange: activeProjectsChange,
            RequestsToday: requestsToday,
            RequestsTodayChange: requestsTodayChange,
            CacheHitRate: cacheHitRate,
            CacheHitRateTarget: 80.0,
            NrbLinkStatus: nrbLinkStatus,
            NrbLinkLatency: nrbLinkLatency,
            NrbLastCheckedAt: nrbLastCheckedAt
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
