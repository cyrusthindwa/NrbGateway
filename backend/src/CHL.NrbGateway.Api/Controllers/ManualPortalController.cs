using System.Security.Claims;
using System.Security.Cryptography;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Entities.ManualPortal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Controllers;

public record ManualUserLoginRequest(string Email, string Password);
public record ManualUserLoginResponse(
    bool Requires2Fa,
    Guid? UserId,
    string? Email,
    string? Token,
    Guid? CompanyId,
    string? CompanyName,
    string? Message,
    bool MustChangePassword = false
);
public record ManualVerify2FaRequest(Guid UserId, string Code);
public record ManualResend2FaRequest(Guid UserId);
public record ManualResetPasswordRequest(Guid UserId, string Token, string NewPassword);
public record ManualChangePasswordRequest(string NewPassword);

public record ManualVerifyRequest(string NationalId);
public record ManualLogItemDto(Guid Id, string NationalIdMasked, string ResultStatus, Guid? GatewayRequestId, DateTimeOffset RequestedAt);
public record ManualDashboardMetricsDto(int VerificationsThisMonth, List<ManualLogItemDto> RecentVerifications);
public record PaginatedManualLogDto(List<ManualLogItemDto> Items, int TotalCount, int Page, int PageSize, int TotalPages);

[ApiController]
[Route("api/v1/manual-portal")]
public class ManualPortalController : ControllerBase
{
    private readonly IManualPortalDbContext _manualDbContext;
    private readonly IConfigDbContext _configDbContext;
    private readonly IVerificationService _verificationService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOtpEmailService _emailService;
    private readonly ILogger<ManualPortalController> _logger;

    public ManualPortalController(
        IManualPortalDbContext manualDbContext,
        IConfigDbContext configDbContext,
        IVerificationService verificationService,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IOtpEmailService emailService,
        ILogger<ManualPortalController> logger)
    {
        _manualDbContext = manualDbContext;
        _configDbContext = configDbContext;
        _verificationService = verificationService;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("auth/login")]
    [AllowAnonymous]
    public async Task<ActionResult<ManualUserLoginResponse>> Login(
        [FromBody] ManualUserLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email and password are required." });

            var user = _manualDbContext.ManualUsers
                .FirstOrDefault(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null || user.Status != "ACTIVE" || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password, or account disabled." });
            }

            // Generate 6-digit OTP code for 2FA
            var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var codeHash = _passwordHasher.HashPassword(otpCode);

            // Invalidate any previous unused OTP codes for this user
            var oldCodes = _manualDbContext.ManualUserOtpCodes
                .Where(c => c.ManualUserId == user.Id && !c.Used)
                .ToList();
            foreach (var old in oldCodes)
            {
                old.Used = true;
                _manualDbContext.Update(old);
            }

            _manualDbContext.Add(new ManualUserOtpCode
            {
                ManualUserId = user.Id,
                CodeHash = codeHash,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                Used = false,
                AttemptCount = 0,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _manualDbContext.SaveChangesAsync(cancellationToken);

            // Send OTP email using the exact same mail credentials configured for the portal
            await _emailService.SendOtpAsync(user.Email, otpCode, cancellationToken);
            _logger.LogInformation("2FA OTP verification code sent to {Email}", user.Email);

            return Ok(new ManualUserLoginResponse(
                Requires2Fa: true,
                UserId: user.Id,
                Email: user.Email,
                Token: null,
                CompanyId: null,
                CompanyName: null,
                Message: "A 6-digit verification code has been sent to your email."
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual portal login failed for email {Email}", request?.Email);
            return StatusCode(500, new { message = $"Login error: {ex.Message}" });
        }
    }

    [HttpPost("auth/verify-2fa")]
    [AllowAnonymous]
    public async Task<ActionResult<ManualUserLoginResponse>> Verify2Fa(
        [FromBody] ManualVerify2FaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { message = "User ID and verification code are required." });

            var user = _manualDbContext.ManualUsers.FirstOrDefault(u => u.Id == request.UserId);
            if (user == null || user.Status != "ACTIVE")
                return Unauthorized(new { message = "User account not found or disabled." });

            var now = DateTimeOffset.UtcNow;
            var activeOtp = _manualDbContext.ManualUserOtpCodes
                .Where(c => c.ManualUserId == user.Id && !c.Used)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            if (activeOtp == null)
                return BadRequest(new { message = "No active verification code found. Please request a new code." });

            if (now > activeOtp.ExpiresAt)
            {
                activeOtp.Used = true;
                _manualDbContext.Update(activeOtp);
                await _manualDbContext.SaveChangesAsync(cancellationToken);
                return BadRequest(new { message = "Verification code has expired. Please request a new code." });
            }

            activeOtp.AttemptCount += 1;
            _manualDbContext.Update(activeOtp);
            await _manualDbContext.SaveChangesAsync(cancellationToken);

            if (activeOtp.AttemptCount > 5)
            {
                activeOtp.Used = true;
                _manualDbContext.Update(activeOtp);
                await _manualDbContext.SaveChangesAsync(cancellationToken);
                return BadRequest(new { message = "Too many failed attempts. Please request a new code." });
            }

            if (!_passwordHasher.VerifyPassword(request.Code, activeOtp.CodeHash))
            {
                return BadRequest(new { message = "Invalid verification code. Please check your email and try again." });
            }

            activeOtp.Used = true;
            _manualDbContext.Update(activeOtp);

            user.LastLoginAt = now;
            _manualDbContext.Update(user);
            await _manualDbContext.SaveChangesAsync(cancellationToken);

            var company = _configDbContext.Companies.FirstOrDefault(c => c.Id == user.CompanyId);
            var token = _jwtTokenService.GenerateManualUserToken(user);

            return Ok(new ManualUserLoginResponse(
                Requires2Fa: false,
                UserId: user.Id,
                Email: user.Email,
                Token: token,
                CompanyId: user.CompanyId,
                CompanyName: company?.Name ?? "Company",
                Message: "Sign-in successful.",
                MustChangePassword: user.MustChangePassword
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual portal 2FA verification failed for user {UserId}", request.UserId);
            return StatusCode(500, new { message = $"Verification error: {ex.Message}" });
        }
    }

    [HttpPost("auth/change-password")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ManualChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
                return BadRequest(new { message = "New password must be at least 8 characters long." });

            var (userId, _) = GetUserContext();
            var user = _manualDbContext.ManualUsers.FirstOrDefault(u => u.Id == userId);
            if (user == null || user.Status != "ACTIVE")
                return Unauthorized(new { message = "User not found or account disabled." });

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.MustChangePassword = false;
            _manualDbContext.Update(user);
            await _manualDbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);
            return Ok(new { message = "Password changed successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual portal change password failed");
            return StatusCode(500, new { message = $"Change password error: {ex.Message}" });
        }
    }

    [HttpPost("auth/resend-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> Resend2Fa(
        [FromBody] ManualResend2FaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.UserId == Guid.Empty)
                return BadRequest(new { message = "User ID is required." });

            var user = _manualDbContext.ManualUsers.FirstOrDefault(u => u.Id == request.UserId);
            if (user == null || user.Status != "ACTIVE")
                return Unauthorized(new { message = "User account not found or disabled." });

            var now = DateTimeOffset.UtcNow;
            var latestPending = _manualDbContext.ManualUserOtpCodes
                .Where(c => c.ManualUserId == user.Id && !c.Used)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            if (latestPending != null && now < latestPending.CreatedAt.AddSeconds(60))
            {
                var retryAfter = (int)Math.Ceiling((latestPending.CreatedAt.AddSeconds(60) - now).TotalSeconds);
                return BadRequest(new { message = $"A code was already sent. Please wait {retryAfter}s before requesting a new code." });
            }

            var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var codeHash = _passwordHasher.HashPassword(otpCode);

            var oldCodes = _manualDbContext.ManualUserOtpCodes
                .Where(c => c.ManualUserId == user.Id && !c.Used)
                .ToList();
            foreach (var old in oldCodes)
            {
                old.Used = true;
                _manualDbContext.Update(old);
            }

            _manualDbContext.Add(new ManualUserOtpCode
            {
                ManualUserId = user.Id,
                CodeHash = codeHash,
                ExpiresAt = now.AddMinutes(10),
                Used = false,
                AttemptCount = 0,
                CreatedAt = now
            });
            await _manualDbContext.SaveChangesAsync(cancellationToken);

            await _emailService.SendOtpAsync(user.Email, otpCode, cancellationToken);
            _logger.LogInformation("Resent 2FA OTP code to {Email}", user.Email);

            return Ok(new { message = "A new verification code has been sent to your email." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend 2FA failed for user {UserId}", request.UserId);
            return StatusCode(500, new { message = $"Resend error: {ex.Message}" });
        }
    }

    [HttpPost("auth/reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ManualResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new { message = "User ID, reset token and new password are required." });

            if (request.NewPassword.Length < 8)
                return BadRequest(new { message = "New password must be at least 8 characters." });

            var user = _manualDbContext.ManualUsers.FirstOrDefault(u => u.Id == request.UserId);
            if (user == null || user.PasswordResetTokenHash == null || user.PasswordResetExpiresAt == null)
                return BadRequest(new { message = "This password reset link is invalid or has expired. Please request a new one." });

            if (DateTimeOffset.UtcNow > user.PasswordResetExpiresAt.Value)
                return BadRequest(new { message = "This password reset link has expired. Please request a new one." });

            if (!_passwordHasher.VerifyPassword(request.Token, user.PasswordResetTokenHash))
                return BadRequest(new { message = "This password reset link is invalid. Please request a new one." });

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.PasswordResetTokenHash = null;
            user.PasswordResetExpiresAt = null;
            _manualDbContext.Update(user);
            await _manualDbContext.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Password reset successful. You can now sign in with your new password." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual portal password reset failed for user {UserId}", request.UserId);
            return StatusCode(500, new { message = $"Password reset error: {ex.Message}" });
        }
    }

    [HttpGet("dashboard")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public ActionResult<ManualDashboardMetricsDto> GetDashboard(CancellationToken cancellationToken)
    {
        var (userId, companyId) = GetUserContext();

        var now = DateTimeOffset.UtcNow;
        var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var verificationsThisMonth = _manualDbContext.ManualVerificationLogs
            .Count(l => l.ManualUserId == userId && l.RequestedAt >= startOfMonth);

        var recent = _manualDbContext.ManualVerificationLogs
            .Where(l => l.ManualUserId == userId)
            .OrderByDescending(l => l.RequestedAt)
            .Take(5)
            .Select(l => new ManualLogItemDto(l.Id, l.NationalIdMasked, l.ResultStatus, l.GatewayRequestId, l.RequestedAt))
            .ToList();

        return Ok(new ManualDashboardMetricsDto(verificationsThisMonth, recent));
    }

    [HttpPost("verify")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<TextLookupResultDto>> Verify(
        [FromBody] ManualVerifyRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NationalId))
            return BadRequest(new { message = "National ID number is required." });

        var (userId, companyId) = GetUserContext();

        // Find internal project for user's company (manual portal project or first available project)
        var project = _configDbContext.Projects
            .FirstOrDefault(p => p.CompanyId == companyId && p.ProjectType == "MANUAL_PORTAL")
            ?? _configDbContext.Projects.FirstOrDefault(p => p.CompanyId == companyId);

        if (project == null)
        {
            return BadRequest(new { message = "No internal project configured for this company. Please contact ICT support." });
        }

        try
        {
            var result = await _verificationService.TextLookupAsync(
                project.Id, project.ShortCode, new TextLookupRequestDto(request.NationalId), cancellationToken);

            var maskedPin = MaskNationalId(request.NationalId);
            var statusStr = result.Found ? (result.CardStatus ?? "VALID RECORD") : "NOT FOUND";

            var logEntry = new ManualVerificationLog
            {
                Id = Guid.NewGuid(),
                ManualUserId = userId,
                CompanyId = companyId,
                NationalIdMasked = maskedPin,
                ResultStatus = statusStr,
                GatewayRequestId = result.VerificationId,
                RequestedAt = DateTimeOffset.UtcNow
            };

            _manualDbContext.Add(logEntry);
            await _manualDbContext.SaveChangesAsync(cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual verification failed for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while processing the verification." });
        }
    }

    [HttpGet("history")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public ActionResult<PaginatedManualLogDto> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var (userId, companyId) = GetUserContext();

        var query = _manualDbContext.ManualVerificationLogs
            .Where(l => l.ManualUserId == userId);

        if (dateFrom.HasValue) query = query.Where(l => l.RequestedAt >= dateFrom.Value);
        if (dateTo.HasValue) query = query.Where(l => l.RequestedAt <= dateTo.Value);

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var items = query
            .OrderByDescending(l => l.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new ManualLogItemDto(l.Id, l.NationalIdMasked, l.ResultStatus, l.GatewayRequestId, l.RequestedAt))
            .ToList();

        return Ok(new PaginatedManualLogDto(items, totalCount, page, pageSize, totalPages));
    }

    private (Guid userId, Guid companyId) GetUserContext()
    {
        var subClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var companyClaim = User.FindFirst("CompanyId")?.Value;

        if (!Guid.TryParse(subClaim, out var userId) || !Guid.TryParse(companyClaim, out var companyId))
        {
            throw new UnauthorizedAccessException("Invalid user context in token claims.");
        }

        return (userId, companyId);
    }

    private static string MaskNationalId(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin)) return "****";
        if (pin.Length <= 4) return pin;
        return new string('*', pin.Length - 4) + pin[^4..];
    }
}
