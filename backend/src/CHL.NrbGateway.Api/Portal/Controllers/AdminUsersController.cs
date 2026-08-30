using System.Security.Claims;
using System.Security.Cryptography;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Portal.Controllers;

[ApiController]
[Route("api/v1/portal/admin-users")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AdminUsersController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOtpEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        IConfigDbContext configDbContext,
        IPasswordHasher passwordHasher,
        IOtpEmailService emailService,
        IConfiguration configuration,
        ILogger<AdminUsersController> logger)
    {
        _configDbContext = configDbContext;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<PaginatedResponseDto<AdminUserDto>> GetAdminUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _configDbContext.AdminUsers.AsQueryable();
        var total = query.Count();

        var data = query
            .OrderBy(a => a.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AdminUserDto(
                a.Id,
                a.Name,
                a.Email,
                a.Status.ToString(),
                a.CreatedAt
            ))
            .ToList();

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        if (totalPages < 1) totalPages = 1;

        return Ok(new PaginatedResponseDto<AdminUserDto>(
            Data: data,
            Total: total,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages
        ));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminUserDto>> CreateAdminUser(
        [FromBody] CreateAdminUserDto dto,
        CancellationToken cancellationToken)
    {
        var email = dto.Email.Trim().ToLower();

        if (_configDbContext.AdminUsers.Any(a => a.Email.ToLower() == email))
            return BadRequest(new { message = "An admin with this email already exists." });

        var admin = new AdminUser
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            Status = AdminStatus.ACTIVE,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _configDbContext.Add(admin);
        AddAuditLog(CurrentAdminId(), SettingArea.ADMIN_USER, $"admin.{admin.Email}.created", null, admin.Email);
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAdminUsers),
            new AdminUserDto(admin.Id, admin.Name, admin.Email, admin.Status.ToString(), admin.CreatedAt));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> UpdateAdminUser(
        Guid id,
        [FromBody] UpdateAdminUserDto dto,
        CancellationToken cancellationToken)
    {
        var admin = _configDbContext.AdminUsers.FirstOrDefault(a => a.Id == id);
        if (admin == null) return NotFound(new { message = "Admin not found." });

        var email = dto.Email.Trim().ToLower();
        if (_configDbContext.AdminUsers.Any(a => a.Id != id && a.Email.ToLower() == email))
            return BadRequest(new { message = "Another admin already uses this email." });

        var oldName = admin.Name;
        var oldEmail = admin.Email;

        admin.Name = dto.Name.Trim();
        admin.Email = dto.Email.Trim();
        admin.UpdatedAt = DateTimeOffset.UtcNow;
        _configDbContext.Update(admin);

        AddAuditLog(CurrentAdminId(), SettingArea.ADMIN_USER, $"admin.{id}.updated",
            $"name={oldName}, email={oldEmail}", $"name={admin.Name}, email={admin.Email}");
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return Ok(new AdminUserDto(admin.Id, admin.Name, admin.Email, admin.Status.ToString(), admin.CreatedAt));
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> UpdateAdminStatus(
        Guid id,
        [FromBody] UpdateAdminStatusDto dto,
        CancellationToken cancellationToken)
    {
        var admin = _configDbContext.AdminUsers.FirstOrDefault(a => a.Id == id);
        if (admin == null) return NotFound(new { message = "Admin not found." });

        var currentAdminId = CurrentAdminId();

        if (dto.Status == AdminStatus.DISABLED && id == currentAdminId)
            return BadRequest(new { message = "You cannot disable your own account." });

        if (dto.Status == AdminStatus.DISABLED)
        {
            var otherActive = _configDbContext.AdminUsers.Any(a => a.Id != id && a.Status == AdminStatus.ACTIVE);
            if (!otherActive)
                return BadRequest(new { message = "At least one active admin must remain." });
        }

        var oldStatus = admin.Status;
        admin.Status = dto.Status;
        admin.UpdatedAt = DateTimeOffset.UtcNow;
        _configDbContext.Update(admin);

        AddAuditLog(currentAdminId, SettingArea.ADMIN_USER, $"admin.{id}.status", oldStatus.ToString(), dto.Status.ToString());
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return Ok(new AdminUserDto(admin.Id, admin.Name, admin.Email, admin.Status.ToString(), admin.CreatedAt));
    }

    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken cancellationToken)
    {
        var admin = _configDbContext.AdminUsers.FirstOrDefault(a => a.Id == id);
        if (admin == null) return NotFound(new { message = "Admin not found." });

        var token = GenerateResetToken();
        admin.PasswordResetTokenHash = _passwordHasher.HashPassword(token);
        admin.PasswordResetExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        admin.UpdatedAt = DateTimeOffset.UtcNow;
        _configDbContext.Update(admin);

        AddAuditLog(CurrentAdminId(), SettingArea.ADMIN_USER, $"admin.{id}.password_reset_requested", null, admin.Email);
        await _configDbContext.SaveChangesAsync(cancellationToken);

        var baseUrl = _configuration["Portal:BaseUrl"] ?? "http://localhost:3000";
        var resetUrl = $"{baseUrl.TrimEnd('/')}/reset-password?adminId={id}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendPasswordResetEmailAsync(admin.Email, resetUrl, cancellationToken);

        return Ok(new { message = "A password reset link has been sent to the admin's email." });
    }

    private void AddAuditLog(Guid adminId, SettingArea area, string key, string? oldValue, string newValue)
    {
        _configDbContext.Add(new ConfigAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
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
