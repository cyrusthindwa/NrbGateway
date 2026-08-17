using CHL.NrbGateway.Api.Gateway.Authentication;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Gateway.Controllers;

[ApiController]
[Route("api/v1/gateway/[controller]")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
public class VerificationController : ControllerBase
{
    private readonly IVerificationService _verificationService;
    private readonly ILogger<VerificationController> _logger;

    public VerificationController(
        IVerificationService verificationService,
        ILogger<VerificationController> logger)
    {
        _verificationService = verificationService;
        _logger = logger;
    }

    private (Guid subsidiaryId, string shortCode) ExtractSubsidiaryContext()
    {
        var idClaim = User.FindFirst("SubsidiaryId")?.Value;
        var codeClaim = User.FindFirst("SubsidiaryShortCode")?.Value;
        if (!Guid.TryParse(idClaim, out var subId))
            throw new UnauthorizedAccessException("Invalid subsidiary context in API key authentication.");
        return (subId, codeClaim ?? "SUB");
    }

    // ── Intermediate (Tier 3) ────────────────────────────────────────

    [HttpPost("intermediate")]
    [ProducesResponseType(typeof(IntermediateVerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IntermediateVerificationResultDto>> VerifyIntermediate(
        [FromBody] IntermediateVerificationRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var (subId, code) = ExtractSubsidiaryContext();
            return Ok(await _verificationService.VerifyIntermediateAsync(subId, code, request, cancellationToken));
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Intermediate verification failed."); return StatusCode(500); }
    }

    // ── Basic (Tier 1) — Always-live field reconciliation ────────────

    [HttpPost("basic")]
    [ProducesResponseType(typeof(BasicVerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BasicVerificationResultDto>> VerifyBasic(
        [FromBody] BasicVerificationRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var (subId, code) = ExtractSubsidiaryContext();
            var result = await _verificationService.VerifyBasicAsync(subId, code, request, cancellationToken);
            if (string.Equals(result.CardStatus, "NOT FOUND", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = "National ID not found in NRB registry.", cardStatus = result.CardStatus });
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Basic verification failed."); return StatusCode(500); }
    }

    // ── Text Lookup (Tier 2) — Demographic retrieval ─────────────────

    [HttpPost("text-lookup")]
    [ProducesResponseType(typeof(TextLookupResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TextLookupResultDto>> TextLookup(
        [FromBody] TextLookupRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var (subId, code) = ExtractSubsidiaryContext();
            var result = await _verificationService.TextLookupAsync(subId, code, request, cancellationToken);
            if (!result.Found)
                return NotFound(new { message = "National ID not found in NRB registry." });
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Text Lookup failed."); return StatusCode(500); }
    }

    // ── Advanced (Tier 4) — Biometric + OTP, two-phase ───────────────

    [HttpPost("advanced")]
    [ProducesResponseType(typeof(AdvancedVerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AdvancedVerificationResultDto>> VerifyAdvanced(
        [FromBody] AdvancedVerificationRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var (subId, code) = ExtractSubsidiaryContext();
            return Ok(await _verificationService.VerifyAdvancedAsync(subId, code, request, cancellationToken));
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Advanced verification failed."); return StatusCode(500); }
    }
}
