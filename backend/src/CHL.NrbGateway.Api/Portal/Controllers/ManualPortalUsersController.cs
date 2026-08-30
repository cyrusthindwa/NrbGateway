using System.Security.Claims;
using System.Security.Cryptography;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Entities.ManualPortal;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Portal.Controllers;

/// <summary>
/// Lets portal admins manage the staff accounts that sign in to the Manual
/// Verification Portal (the human-in-the-loop KYC lookup interface).
/// </summary>
[ApiController]
[Route("api/v1/portal/manual-portal-users")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ManualPortalUsersController : ControllerBase
{
    private readonly IManualPortalDbContext _manualDbContext;
    private readonly IConfigDbContext _configDbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOtpEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ManualPortalUsersController> _logger;

    public ManualPortalUsersController(
        IManualPortalDbContext manualDbContext,
        IConfigDbContext configDbContext,
        IPasswordHasher passwordHasher,
        IOtpEmailService emailService,
        IConfiguration configuration,
        ILogger<ManualPortalUsersController> logger)
    {
        _manualDbContext = manualDbContext;
        _configDbContext = configDbContext;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ManualPortalUserDto>> GetUsers()
    {
        var companies = _configDbContext.Companies.ToDictionary(c => c.Id, c => c.Name);

        var users = _manualDbContext.ManualUsers
            .OrderBy(u => u.Email)
            .ToList()
            .Select(u => new ManualPortalUserDto(
                u.Id,
                u.Email,
                u.CompanyId,
                companies.TryGetValue(u.CompanyId, out var name) ? name : "Unknown company",
                u.Status,
                u.CreatedAt,
                u.LastLoginAt
            ))
            .ToList();

        return Ok(users);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ManualPortalUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ManualPortalUserDto>> CreateUser(
        [FromBody] CreateManualPortalUserDto dto,
        CancellationToken cancellationToken)
    {
        var email = dto.Email.Trim();

        if (_manualDbContext.ManualUsers.Any(u => u.Email.ToLower() == email.ToLower()))
            return BadRequest(new { message = "A manual portal user with this email already exists." });

        var company = _configDbContext.Companies.FirstOrDefault(c => c.Id == dto.CompanyId);
        if (company == null)
            return BadRequest(new { message = "The selected company does not exist." });

        var user = new ManualUser
        {
            Id = Guid.NewGuid(),
            CompanyId = dto.CompanyId,
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            Status = "ACTIVE",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _manualDbContext.Add(user);
        AddAuditLog(SettingArea.MANUAL_PORTAL_USER, $"manual_user.{email}.created", null, email);
        await _manualDbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetUsers),
            new ManualPortalUserDto(user.Id, user.Email, user.CompanyId, company.Name, user.Status, user.CreatedAt, user.LastLoginAt));
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ManualPortalUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManualPortalUserDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateManualPortalUserStatusDto dto,
        CancellationToken cancellationToken)
    {
        var user = _manualDbContext.ManualUsers.FirstOrDefault(u => u.Id == id);
        if (user == null) return NotFound(new { message = "Manual portal user not found." });

        var status = dto.Status.ToUpperInvariant();
        if (status != "ACTIVE" && status != "DISABLED")
            return BadRequest(new { message = "Status must be ACTIVE or DISABLED." });

        var oldStatus = user.Status;
        user.Status = status;
        _manualDbContext.Update(user);

        AddAuditLog(SettingArea.MANUAL_PORTAL_USER, $"manual_user.{id}.status", oldStatus, status);
        await _manualDbContext.SaveChangesAsync(cancellationToken);

        var company = _configDbContext.Companies.FirstOrDefault(c => c.Id == user.CompanyId);
        return Ok(new ManualPortalUserDto(user.Id, user.Email, user.CompanyId, company?.Name ?? "Unknown company", user.Status, user.CreatedAt, user.LastLoginAt));
    }

    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken cancellationToken)
    {
        var user = _manualDbContext.ManualUsers.FirstOrDefault(u => u.Id == id);
        if (user == null) return NotFound(new { message = "Manual portal user not found." });

        var token = GenerateResetToken();
        user.PasswordResetTokenHash = _passwordHasher.HashPassword(token);
        user.PasswordResetExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        _manualDbContext.Update(user);

        AddAuditLog(SettingArea.MANUAL_PORTAL_USER, $"manual_user.{id}.password_reset_requested", null, user.Email);
        await _manualDbContext.SaveChangesAsync(cancellationToken);

        var baseUrl = _configuration["ManualPortal:BaseUrl"] ?? "http://localhost:3001";
        var resetUrl = $"{baseUrl.TrimEnd('/')}/reset-password?userId={id}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendPasswordResetEmailAsync(user.Email, resetUrl, cancellationToken);

        return Ok(new { message = "A password reset link has been sent to the user's email." });
    }

    private void AddAuditLog(SettingArea area, string key, string? oldValue, string newValue)
    {
        _configDbContext.Add(new ConfigAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = CurrentAdminId(),
            SettingArea = area,
            SettingKey = key,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedAt = DateTimeOffset.UtcNow
        });
    }

    private Guid CurrentAdminId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static string GenerateResetToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
