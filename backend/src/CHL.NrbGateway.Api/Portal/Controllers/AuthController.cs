using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Portal.Controllers;

[ApiController]
[Route("api/v1/portal/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOtpService _otpService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IConfigDbContext configDbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IOtpService otpService,
        ILogger<AuthController> logger)
    {
        _configDbContext = configDbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _otpService = otpService;
        _logger = logger;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(OtpChallengeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<OtpChallengeDto>> Login(
        [FromBody] AdminLoginDto request,
        CancellationToken cancellationToken)
    {
        // Seed default admin if table is empty for development ease
        if (!_configDbContext.AdminUsers.Any())
        {
            var seedAdmin = new AdminUser
            {
                Id = Guid.NewGuid(),
                Name = "CHL ICT Administrator",
                Email = "admin@continental.mw",
                PasswordHash = _passwordHasher.HashPassword("Admin123!"),
                Status = AdminStatus.ACTIVE,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _configDbContext.Add(seedAdmin);
            await _configDbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded initial ICT Admin User: admin@continental.mw");
        }

        var admin = _configDbContext.AdminUsers
            .FirstOrDefault(a => a.Email.ToLower() == request.Email.ToLower());

        if (admin == null || admin.Status != AdminStatus.ACTIVE || !_passwordHasher.VerifyPassword(request.Password, admin.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid admin credentials or account disabled." });
        }

        admin.LastLoginAt = DateTimeOffset.UtcNow;
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return await IssueOtpChallengeAsync(admin, cancellationToken);
    }

    [HttpPost("login/verify-otp")]
    [ProducesResponseType(typeof(AdminLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AdminLoginResponseDto>> VerifyOtp(
        [FromBody] VerifyOtpDto request,
        CancellationToken cancellationToken)
    {
        var admin = _configDbContext.AdminUsers.FirstOrDefault(a => a.Id == request.AdminId);
        if (admin == null || admin.Status != AdminStatus.ACTIVE)
            return Unauthorized(new { message = "Admin account not found or disabled." });

        var result = await _otpService.VerifyAsync(admin.Id, request.Code, cancellationToken);

        if (result.Status == OtpVerifyStatus.Success)
        {
            var token = _jwtTokenService.GenerateToken(admin);
            return Ok(new AdminLoginResponseDto(
                Token: token,
                AdminId: admin.Id,
                Name: admin.Name,
                Email: admin.Email
            ));
        }

        if (result.Status == OtpVerifyStatus.TooManyAttempts)
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = result.Message });

        return BadRequest(new { message = result.Message });
    }

    [HttpPost("login/resend-otp")]
    [ProducesResponseType(typeof(OtpChallengeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<OtpChallengeDto>> ResendOtp(
        [FromBody] ResendOtpDto request,
        CancellationToken cancellationToken)
    {
        var admin = _configDbContext.AdminUsers.FirstOrDefault(a => a.Id == request.AdminId);
        if (admin == null || admin.Status != AdminStatus.ACTIVE)
            return Unauthorized(new { message = "Admin account not found or disabled." });

        return await IssueOtpChallengeAsync(admin, cancellationToken);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var admin = _configDbContext.AdminUsers.FirstOrDefault(a => a.Id == request.AdminId);

        if (admin == null || string.IsNullOrEmpty(admin.PasswordResetTokenHash) || admin.PasswordResetExpiresAt == null)
            return BadRequest(new { message = "This reset link is invalid or has already been used." });

        if (DateTimeOffset.UtcNow > admin.PasswordResetExpiresAt.Value)
            return BadRequest(new { message = "This reset link has expired. Please request a new one." });

        if (!_passwordHasher.VerifyPassword(request.Token, admin.PasswordResetTokenHash))
            return BadRequest(new { message = "This reset link is invalid." });

        admin.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        admin.PasswordResetTokenHash = null;
        admin.PasswordResetExpiresAt = null;
        admin.UpdatedAt = DateTimeOffset.UtcNow;
        _configDbContext.Update(admin);
        await _configDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} completed a password reset.", admin.Id);

        return Ok(new { message = "Password has been reset. You can now sign in." });
    }

    private async Task<ActionResult<OtpChallengeDto>> IssueOtpChallengeAsync(
        AdminUser admin,
        CancellationToken cancellationToken)
    {
        var result = await _otpService.IssueAsync(admin, cancellationToken);

        return result.Status switch
        {
            OtpIssueStatus.Issued => Ok(new OtpChallengeDto(
                AdminId: admin.Id,
                ExpiresInSeconds: result.ExpiresInSeconds ?? 0,
                Message: "A verification code has been sent to your email.")),
            OtpIssueStatus.RateLimited => StatusCode(StatusCodes.Status429TooManyRequests,
                new { message = result.Message, retryAfterSeconds = result.RetryAfterSeconds }),
            _ => StatusCode(500, new { message = "Unexpected OTP issue result." })
        };
    }
}
