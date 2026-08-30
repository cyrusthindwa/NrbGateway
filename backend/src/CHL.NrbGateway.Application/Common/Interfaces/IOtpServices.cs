using CHL.NrbGateway.Domain.Entities.Config;

namespace CHL.NrbGateway.Application.Common.Interfaces;

/// <summary>
/// Delivers one-time passcodes and password-reset emails to an administrator over SMTP.
/// </summary>
public interface IOtpEmailService
{
    Task SendOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string toEmail, string resetUrl, CancellationToken cancellationToken = default);
}

public enum OtpIssueStatus
{
    Issued,
    RateLimited
}

public enum OtpVerifyStatus
{
    Success,
    InvalidCode,
    Expired,
    TooManyAttempts
}

public record OtpIssueResult(
    OtpIssueStatus Status,
    int? ExpiresInSeconds,
    int? RetryAfterSeconds,
    string? Message);

public record OtpVerifyResult(
    OtpVerifyStatus Status,
    string? Message);

/// <summary>
/// Issues and verifies time-limited one-time passcodes for administrator 2FA.
/// Codes are stored hashed (never plaintext) and are rate-limited.
/// </summary>
public interface IOtpService
{
    Task<OtpIssueResult> IssueAsync(AdminUser admin, CancellationToken cancellationToken = default);
    Task<OtpVerifyResult> VerifyAsync(Guid adminId, string code, CancellationToken cancellationToken = default);
}
