using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Portal.Controllers;

[ApiController]
[Route("api/v1/portal/nrb-status")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NrbStatusController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;

    public NrbStatusController(IConfigDbContext configDbContext)
    {
        _configDbContext = configDbContext;
    }

    [HttpGet]
    public ActionResult<NrbStatusDto> GetStatus()
    {
        var latest = _configDbContext.NrbHealthChecks
            .OrderByDescending(h => h.CheckedAt)
            .FirstOrDefault();

        if (latest == null)
        {
            return Ok(new NrbStatusDto("Not yet monitored", null, null, null, null, null));
        }

        var openIncident = _configDbContext.NrbDowntimeIncidents
            .Where(i => i.EndedAt == null)
            .OrderByDescending(i => i.StartedAt)
            .Select(i => new NrbDowntimeIncidentDto(
                i.Id, i.StartedAt, i.EndedAt, i.DetectedBy.ToString(), i.Notified,
                i.ResolvedBy, i.ResolvedByAdmin != null ? i.ResolvedByAdmin.Name : null))
            .FirstOrDefault();

        var status = latest.IsUp ? "Healthy" : "Down";

        return Ok(new NrbStatusDto(status, latest.IsUp, latest.LatencyMs, latest.ErrorMessage, latest.CheckedAt, openIncident));
    }

    [HttpGet("incidents")]
    public ActionResult<IEnumerable<NrbDowntimeIncidentDto>> GetIncidents()
    {
        var incidents = _configDbContext.NrbDowntimeIncidents
            .OrderByDescending(i => i.StartedAt)
            .Take(100)
            .Select(i => new NrbDowntimeIncidentDto(
                i.Id, i.StartedAt, i.EndedAt, i.DetectedBy.ToString(), i.Notified,
                i.ResolvedBy, i.ResolvedByAdmin != null ? i.ResolvedByAdmin.Name : null))
            .ToList();

        return Ok(incidents);
    }
}
