using CHL.NrbGateway.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Controllers;

[ApiController]
[Route("health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly IKycDbContext _kycDbContext;
    private readonly IConfigDbContext _configDbContext;

    public HealthController(IKycDbContext kycDbContext, IConfigDbContext configDbContext)
    {
        _kycDbContext = kycDbContext;
        _configDbContext = configDbContext;
    }

    [HttpGet]
    public IActionResult GetHealth()
    {
        bool kycCanConnect = false;
        bool configCanConnect = false;

        try
        {
            kycCanConnect = _kycDbContext.Individuals.Any() || true;
        }
        catch
        {
            kycCanConnect = false;
        }

        try
        {
            configCanConnect = _configDbContext.AdminUsers.Any() || true;
        }
        catch
        {
            configCanConnect = false;
        }

        var healthStatus = new
        {
            status = "Healthy",
            timestamp = DateTimeOffset.UtcNow,
            service = "CHL NRB Verification Gateway",
            reference = "CICT/10032601/NRB",
            checks = new
            {
                kyc_database = kycCanConnect ? "Healthy" : "Degraded",
                config_database = configCanConnect ? "Healthy" : "Degraded"
            }
        };

        return Ok(healthStatus);
    }
}
