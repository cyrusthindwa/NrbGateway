using System.Security.Claims;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
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
    private readonly IConfigDbContext _configDbContext;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        IVerificationService verificationService,
        IConfigDbContext configDbContext,
        ILogger<MaintenanceController> logger)
    {
        _verificationService = verificationService;
        _configDbContext = configDbContext;
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

        // Guard against stale sessions: a valid JWT can reference an admin account
        // that no longer exists (e.g. after a schema reset). RevalidateAllAsync inserts
        // RevalidationBatch.InitiatedBy with a FK to admin_users, which would otherwise
        // surface as an unhelpful 500 FK violation.
        if (!_configDbContext.AdminUsers.Any(a => a.Id == adminId))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Your admin session is no longer valid. Please log out and sign in again." });
        }

        try
        {
            _logger.LogInformation("Revalidation triggered by admin {AdminId}", adminId);
            var result = await _verificationService.RevalidateAllAsync(adminId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revalidation batch failed.");
            return StatusCode(500, new { message = "Revalidation failed unexpectedly." });
        }
    }

    [HttpGet("revalidation-batches")]
    public ActionResult<IEnumerable<RevalidationBatchDto>> GetRevalidationBatches()
    {
        var batches = _configDbContext.RevalidationBatches
            .OrderByDescending(b => b.StartedAt)
            .Take(50)
            .Select(b => new RevalidationBatchDto(
                b.Id, b.TriggerType, b.InitiatedBy,
                b.Initiator != null ? b.Initiator.Name : null,
                b.StartedAt, b.CompletedAt,
                b.TotalCount, b.ValidCount, b.ExpiredCount, b.DeceasedCount, b.SeeNrbCount, b.ErrorCount))
            .ToList();

        return Ok(batches);
    }

    [HttpGet("revalidation-batches/{id:guid}")]
    public ActionResult<RevalidationBatchDto> GetRevalidationBatch(Guid id)
    {
        var b = _configDbContext.RevalidationBatches.FirstOrDefault(x => x.Id == id);
        if (b == null) return NotFound(new { message = "Revalidation batch not found." });

        return Ok(new RevalidationBatchDto(
            b.Id, b.TriggerType, b.InitiatedBy,
            b.Initiator != null ? b.Initiator.Name : null,
            b.StartedAt, b.CompletedAt,
            b.TotalCount, b.ValidCount, b.ExpiredCount, b.DeceasedCount, b.SeeNrbCount, b.ErrorCount));
    }
}
