using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CHL.NrbGateway.Infrastructure.Services;

/// <summary>
/// Dispatches detailed diagnostic system error alerts to configured notification channels.
/// Includes automatic deduplication/debouncing to prevent inbox storms during high error rates.
/// </summary>
public class ErrorNotifierService : IErrorNotifierService
{
    private readonly IConfigDbContext _configDbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ErrorNotifierService> _logger;

    // Cache of error signatures to prevent notification storms (debounce window: 60 seconds)
    private static readonly ConcurrentDictionary<string, DateTimeOffset> _recentErrors = new();
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(60);

    public ErrorNotifierService(
        IConfigDbContext configDbContext,
        IConfiguration configuration,
        ILogger<ErrorNotifierService> logger)
    {
        _configDbContext = configDbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task NotifyErrorAsync(
        Exception exception,
        SystemErrorContext? context = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Clean expired debounces periodically
            var now = DateTimeOffset.UtcNow;
            foreach (var kvp in _recentErrors)
            {
                if (now - kvp.Value > DebounceWindow)
                {
                    _recentErrors.TryRemove(kvp.Key, out _);
                }
            }

            // Deduplication signature
            var signature = $"{exception.GetType().FullName}:{exception.Message}:{context?.RequestPath}:{context?.HttpMethod}";
            if (_recentErrors.TryGetValue(signature, out var lastSent) && (now - lastSent) < DebounceWindow)
            {
                _logger.LogInformation("System error email notification suppressed due to 60s debounce: {Signature}", signature);
                return;
            }

            _recentErrors[signature] = now;

            // Fetch active email notification channels
            var recipients = _configDbContext.NotificationChannels
                .Where(c => c.ChannelType == NotificationChannelType.EMAIL && c.Enabled)
                .Select(c => c.Target.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Fallback recipient from config if no channels configured
            if (!recipients.Any())
            {
                var fallback = _configuration["Mail:ErrorAlertRecipient"];
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    recipients.Add(fallback.Trim());
                }
            }

            if (!recipients.Any())
            {
                _logger.LogWarning("System error occurred but no enabled EMAIL notification channels or fallback recipients are configured.");
                return;
            }

            var subject = $"[SYSTEM ALERT] NRB Gateway Error: {exception.GetType().Name} on {context?.RequestPath ?? "Background"}";
            var bodyHtml = BuildErrorEmailHtml(exception, context, now);

            await SendEmailToRecipientsAsync(recipients, subject, bodyHtml, cancellationToken);
        }
        catch (Exception ex)
        {
            // Error notifier must never throw or disrupt execution
            _logger.LogError(ex, "Failed to send system error notification email.");
        }
    }

    private async Task SendEmailToRecipientsAsync(
        List<string> recipients,
        string subject,
        string bodyHtml,
        CancellationToken cancellationToken)
    {
        var host = _configuration["Mail:Host"];
        var username = _configuration["Mail:Username"];
        var password = _configuration["Mail:Password"];
        var fromAddress = _configuration["Mail:FromAddress"];
        var fromName = _configuration["Mail:FromName"] ?? "NRB Gateway Alert System";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("SMTP credentials not configured — system error email alert was not sent.");
            return;
        }

        var port = int.TryParse(_configuration["Mail:Port"], out var p) && p > 0 ? p : 587;

        using var message = new MailMessage
        {
            From = new MailAddress(string.IsNullOrWhiteSpace(fromAddress) ? username : fromAddress, fromName),
            Subject = subject,
            IsBodyHtml = true,
            Body = bodyHtml
        };

        foreach (var recipient in recipients)
        {
            message.To.Add(recipient);
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("System error email alert dispatched to {RecipientCount} recipients: {Subject}", recipients.Count, subject);
    }

    private static string BuildErrorEmailHtml(Exception ex, SystemErrorContext? ctx, DateTimeOffset timestamp)
    {
        var safeMessage = WebUtility.HtmlEncode(ex.Message);
        var safeType = WebUtility.HtmlEncode(ex.GetType().FullName ?? ex.GetType().Name);
        var safeStackTrace = WebUtility.HtmlEncode(ex.ToString());

        var reqPath = WebUtility.HtmlEncode(ctx?.RequestPath ?? "N/A");
        var reqMethod = WebUtility.HtmlEncode(ctx?.HttpMethod ?? "N/A");
        var clientIp = WebUtility.HtmlEncode(ctx?.ClientIp ?? "Unknown");
        var userOrProject = WebUtility.HtmlEncode(ctx?.UserOrProject ?? "Anonymous / Unauthenticated");
        var queryString = WebUtility.HtmlEncode(ctx?.QueryString ?? "None");
        var statusCode = ctx?.StatusCode?.ToString() ?? "500 Internal Server Error";

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
</head>
<body style=""margin:0;padding:0;background:#f8fafc;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:#1e293b;"">
    <div style=""max-width:760px;margin:24px auto;background:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1),0 2px 4px -2px rgba(0,0,0,0.1);border:1px solid #e2e8f0;"">
        <!-- Header -->
        <div style=""background:#dc2626;padding:20px 28px;color:#ffffff;"">
            <div style=""font-size:12px;text-transform:uppercase;letter-spacing:1px;font-weight:700;opacity:0.9;"">Continental Holdings Limited &bull; NRB Gateway</div>
            <h1 style=""margin:6px 0 0;font-size:20px;font-weight:700;"">System Error Diagnostic Report</h1>
        </div>

        <!-- Content -->
        <div style=""padding:28px;"">
            <div style=""background:#fef2f2;border-left:4px solid #dc2626;padding:14px 18px;border-radius:4px;margin-bottom:24px;"">
                <div style=""font-size:12px;color:#991b1b;font-weight:700;text-transform:uppercase;letter-spacing:0.5px;"">{safeType}</div>
                <div style=""font-size:16px;color:#7f1d1d;font-weight:600;margin-top:4px;"">{safeMessage}</div>
            </div>

            <h3 style=""font-size:14px;text-transform:uppercase;letter-spacing:0.5px;color:#64748b;margin:0 0 12px;border-bottom:1px solid #f1f5f9;padding-bottom:6px;"">Execution & HTTP Context</h3>
            <table style=""width:100%;border-collapse:collapse;margin-bottom:24px;font-size:13px;"">
                <tr style=""border-bottom:1px solid #f1f5f9;"">
                    <td style=""padding:8px 0;color:#64748b;width:150px;font-weight:600;"">Timestamp:</td>
                    <td style=""padding:8px 0;color:#0f172a;"">{timestamp:yyyy-MM-dd HH:mm:ss} UTC</td>
                </tr>
                <tr style=""border-bottom:1px solid #f1f5f9;"">
                    <td style=""padding:8px 0;color:#64748b;font-weight:600;"">Endpoint / Method:</td>
                    <td style=""padding:8px 0;color:#0f172a;""><code style=""background:#f1f5f9;padding:2px 6px;border-radius:4px;color:#0f172a;"">{reqMethod} {reqPath}</code></td>
                </tr>
                <tr style=""border-bottom:1px solid #f1f5f9;"">
                    <td style=""padding:8px 0;color:#64748b;font-weight:600;"">Status Code:</td>
                    <td style=""padding:8px 0;color:#dc2626;font-weight:600;"">{statusCode}</td>
                </tr>
                <tr style=""border-bottom:1px solid #f1f5f9;"">
                    <td style=""padding:8px 0;color:#64748b;font-weight:600;"">Query String:</td>
                    <td style=""padding:8px 0;color:#475569;"">{queryString}</td>
                </tr>
                <tr style=""border-bottom:1px solid #f1f5f9;"">
                    <td style=""padding:8px 0;color:#64748b;font-weight:600;"">Client IP:</td>
                    <td style=""padding:8px 0;color:#0f172a;"">{clientIp}</td>
                </tr>
                <tr style=""border-bottom:1px solid #f1f5f9;"">
                    <td style=""padding:8px 0;color:#64748b;font-weight:600;"">User / Project:</td>
                    <td style=""padding:8px 0;color:#0f172a;font-weight:600;"">{userOrProject}</td>
                </tr>
            </table>

            <h3 style=""font-size:14px;text-transform:uppercase;letter-spacing:0.5px;color:#64748b;margin:0 0 12px;border-bottom:1px solid #f1f5f9;padding-bottom:6px;"">Full Stack Trace & Diagnostics</h3>
            <pre style=""background:#0f172a;color:#f8fafc;padding:16px;border-radius:6px;overflow-x:auto;font-family:Consolas,Monaco,'Courier New',monospace;font-size:12px;line-height:1.5;white-space:pre-wrap;word-break:break-all;margin:0;"">{safeStackTrace}</pre>
        </div>

        <!-- Footer -->
        <div style=""background:#f8fafc;padding:16px 28px;border-top:1px solid #e2e8f0;font-size:12px;color:#94a3b8;text-align:center;"">
            Automatic diagnostic notification sent to configured notification channels &bull; Continental Holdings Limited
        </div>
    </div>
</body>
</html>";
    }
}
