using System.Security.Cryptography;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Entities.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CHL.NrbGateway.Application.Services;

public class OtpService : IOtpService
{
    private const int ResendCooldownSeconds = 60;

    private readonly IConfigDbContext _configDbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOtpEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IConfigDbContext configDbContext,
        IPasswordHasher passwordHasher,
        IOtpEmailService emailService,
        IConfiguration configuration,
        ILogger<OtpService> logger)
    {
        _configDbContext = configDbContext;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<OtpIssueResult> IssueAsync(AdminUser admin, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expiryMinutes = GetConfigInt("Otp:ExpiryMinutes", 10);
        var expiresInSeconds = expiryMinutes * 60;

        var latestPending = _configDbContext.AdminOtpCodes
            .Where(c => c.AdminId == admin.Id && !c.Used)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault();

        if (latestPending != null && now < latestPending.CreatedAt.AddSeconds(ResendCooldownSeconds))
        {
            var retryAfter = (int)Math.Ceiling((latestPending.CreatedAt.AddSeconds(ResendCooldownSeconds) - now).TotalSeconds);
            return new OtpIssueResult(
                OtpIssueStatus.RateLimited,
                null,
                retryAfter,
                $"A code was already sent. Please wait {retryAfter}s or use the code sent to your email.");
        }

        var code = GenerateCode();
        var codeHash = _passwordHasher.HashPassword(code);

        // Invalidate any previous unused codes so only one is active at a time.
        var pendingCodes = _configDbContext.AdminOtpCodes
            .Where(c => c.AdminId == admin.Id && !c.Used)
            .ToList();

        foreach (var old in pendingCodes)
        {
            old.Used = true;
            _configDbContext.Update(old);
        }

        _configDbContext.Add(new AdminOtpCode
        {
            AdminId = admin.Id,
            CodeHash = codeHash,
            ExpiresAt = now.AddMinutes(expiryMinutes),
            Used = false,
            AttemptCount = 0,
            CreatedAt = now
        });

        await _configDbContext.SaveChangesAsync(cancellationToken);

        await _emailService.SendOtpAsync(admin.Email, code, cancellationToken);

        return new OtpIssueResult(OtpIssueStatus.Issued, expiresInSeconds, null, null);
    }

    public async Task<OtpVerifyResult> VerifyAsync(Guid adminId, string code, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var maxAttempts = GetConfigInt("Otp:MaxAttempts", 5);

        var pending = _configDbContext.AdminOtpCodes
            .Where(c => c.AdminId == adminId && !c.Used)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault();

        if (pending == null)
            return new OtpVerifyResult(OtpVerifyStatus.Expired, "No active code found. Please request a new code.");

        if (now > pending.ExpiresAt)
        {
            pending.Used = true;
            _configDbContext.Update(pending);
            await _configDbContext.SaveChangesAsync(cancellationToken);
            return new OtpVerifyResult(OtpVerifyStatus.Expired, "The code has expired. Please request a new code.");
        }

        pending.AttemptCount += 1;
        _configDbContext.Update(pending);
        await _configDbContext.SaveChangesAsync(cancellationToken);

        if (pending.AttemptCount > maxAttempts)
        {
            pending.Used = true;
            _configDbContext.Update(pending);
            await _configDbContext.SaveChangesAsync(cancellationToken);
            return new OtpVerifyResult(OtpVerifyStatus.TooManyAttempts, "Too many attempts. Please request a new code.");
        }

        if (!_passwordHasher.VerifyPassword(code, pending.CodeHash))
            return new OtpVerifyResult(OtpVerifyStatus.InvalidCode, "Invalid code. Please try again.");

        pending.Used = true;
        _configDbContext.Update(pending);
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return new OtpVerifyResult(OtpVerifyStatus.Success, null);
    }

    private string GenerateCode()
    {
        var length = Math.Clamp(GetConfigInt("Otp:CodeLength", 6), 4, 9);
        var max = (int)Math.Pow(10, length);
        return RandomNumberGenerator.GetInt32(0, max).ToString($"D{length}");
    }

    private int GetConfigInt(string key, int fallback)
    {
        var value = _configuration[key];
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
