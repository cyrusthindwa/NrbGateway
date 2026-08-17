using System.Security.Claims;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Portal.Controllers;

[ApiController]
[Route("api/v1/portal/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MaintenanceController : ControllerBase
{
    private readonly IVerificationService _verificationService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        IVerificationService verificationService,
        ILogger<MaintenanceController> logger)
    {
        _verificationService = verificationService;
        _logger = logger;
    }

    /// <summary>
    /// Re-validate every PIN in the local NRB mirror against NRB Basic tier.
    /// Updates card status and record status for each individual.
    /// Returns a summary of the results.
    /// </summary>
    [HttpPost("revalidate")]
    [ProducesResponseType(typeof(RevalidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RevalidationResultDto>> Revalidate(CancellationToken cancellationToken)
    {
        var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(adminIdClaim, out var adminId))
            return Unauthorized(new { message = "Invalid admin identity." });

        try
        {
            _logger.LogInformation("Revalidation triggered by admin {AdminId}", adminId);
            var result = await _verificationService.RevalidateAllAsync(adminId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revalidation batch failed.");
            return StatusCode(500, new { message = "Revalidation failed." });
        }
    }
}
