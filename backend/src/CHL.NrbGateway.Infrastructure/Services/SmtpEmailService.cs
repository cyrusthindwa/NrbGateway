using System.Net;
using System.Net.Mail;
using CHL.NrbGateway.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CHL.NrbGateway.Infrastructure.Services;

/// <summary>
/// Sends admin email (OTP codes, password resets and notifications) over SMTP.
/// </summary>
public class SmtpEmailService : IOtpEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        var inner = $@"
            <h2 style=""margin:0 0 8px;color:#0f172a;font-size:20px;"">Sign-in verification code</h2>
            <p style=""margin:0 0 24px;color:#475569;line-height:1.5;"">Use the code below to complete your sign-in to the NRB Gateway Console. It expires in 10 minutes and can be used once.</p>
            <div style=""background:#f1f5f9;border-radius:8px;padding:20px;text-align:center;margin:0 0 24px;"">
                <span style=""font-size:34px;font-weight:bold;letter-spacing:12px;color:#0f172a;font-family:Consolas,monospace;"">{otpCode}</span>
            </div>
            <p style=""margin:0;color:#94a3b8;font-size:13px;"">If you did not request this, you can safely ignore this email.</p>";

        return SendAsync(toEmail, "NRB Gateway Console — sign-in verification code", inner, cancellationToken);
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetUrl, CancellationToken cancellationToken = default)
    {
        var inner = $@"
            <h2 style=""margin:0 0 8px;color:#0f172a;font-size:20px;"">Reset your password</h2>
            <p style=""margin:0 0 24px;color:#475569;line-height:1.5;"">A password reset was requested for your NRB Gateway Console account. This link expires shortly and can be used once.</p>
            <p style=""margin:0 0 24px;"">
                <a href=""{resetUrl}"" style=""display:inline-block;background:#f58220;color:#ffffff;text-decoration:none;font-weight:bold;padding:12px 24px;border-radius:8px;"">Set a new password</a>
            </p>
            <p style=""margin:0;color:#94a3b8;font-size:13px;line-height:1.5;"">If the button does not work, copy this link into your browser:<br/>{resetUrl}</p>
            <p style=""margin:24px 0 0;color:#94a3b8;font-size:13px;"">If you did not request this, you can safely ignore this email.</p>";

        return SendAsync(toEmail, "NRB Gateway Console — reset your password", inner, cancellationToken);
    }

    private async Task SendAsync(string toEmail, string subject, string innerHtml, CancellationToken cancellationToken)
    {
        var host = _configuration["Mail:Host"];
        var username = _configuration["Mail:Username"];
        var password = _configuration["Mail:Password"];
        var fromAddress = _configuration["Mail:FromAddress"];
        var fromName = _configuration["Mail:FromName"] ?? "NRB Gateway Console";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("SMTP not configured — email to {To} was not sent.", toEmail);
            return;
        }

        var port = int.TryParse(_configuration["Mail:Port"], out var p) && p > 0 ? p : 587;

        using var message = new MailMessage
        {
            From = new MailAddress(string.IsNullOrWhiteSpace(fromAddress) ? username : fromAddress, fromName),
            Subject = subject,
            IsBodyHtml = true,
            Body = WrapLayout(innerHtml)
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Email sent to {To}: {Subject}", toEmail, subject);
    }

    private static string WrapLayout(string innerHtml)
    {
        var year = DateTimeOffset.UtcNow.Year;
        return $@"<!DOCTYPE html>
<html>
<body style=""margin:0;padding:0;background:#f1f5f9;"">
    <div style=""max-width:560px;margin:0 auto;font-family:Calibri,'Segoe UI',Arial,sans-serif;"">
        <div style=""background:#0f172a;padding:20px 28px;"">
            <span style=""color:#f58220;font-weight:bold;font-size:18px;"">NRB Gateway Console</span>
        </div>
        <div style=""background:#ffffff;padding:28px;color:#1e293b;"">
            {innerHtml}
        </div>
        <div style=""padding:16px 28px;color:#94a3b8;font-size:12px;text-align:center;"">
            &copy; {year} Continental Holdings Limited — ICT Admin
        </div>
    </div>
</body>
</html>";
    }
}
