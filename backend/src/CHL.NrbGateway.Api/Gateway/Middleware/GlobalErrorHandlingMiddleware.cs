using System.Security.Claims;
using CHL.NrbGateway.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CHL.NrbGateway.Api.Gateway.Middleware;

public class GlobalErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;

    public GlobalErrorHandlingMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IErrorNotifierService errorNotifier)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing HTTP request: {Method} {Path}", context.Request.Method, context.Request.Path);

            var userOrProject = ResolveUserOrProject(context);

            var errorContext = new SystemErrorContext(
                RequestPath: context.Request.Path,
                HttpMethod: context.Request.Method,
                QueryString: context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
                ClientIp: context.Connection.RemoteIpAddress?.ToString(),
                UserAgent: context.Request.Headers["User-Agent"].ToString(),
                UserOrProject: userOrProject,
                StatusCode: StatusCodes.Status500InternalServerError
            );

            // Notify configured channels asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await errorNotifier.NotifyErrorAsync(ex, errorContext);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogError(notifyEx, "Failed to dispatch system error notification.");
                }
            });

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    status = 500,
                    error = "Internal Server Error",
                    message = "An unexpected error occurred. System administrators have been notified.",
                    traceId = context.TraceIdentifier
                });
            }
        }
    }

    private static string ResolveUserOrProject(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return "Anonymous / Public";
        }

        var projectShortCode = context.User.FindFirst("project_short_code")?.Value;
        var projectId = context.User.FindFirst("project_id")?.Value;
        if (!string.IsNullOrEmpty(projectShortCode) || !string.IsNullOrEmpty(projectId))
        {
            return $"Project: {projectShortCode ?? projectId}";
        }

        var email = context.User.FindFirst(ClaimTypes.Email)?.Value ?? context.User.FindFirst("email")?.Value;
        var name = context.User.FindFirst(ClaimTypes.Name)?.Value ?? context.User.Identity.Name;

        return !string.IsNullOrEmpty(email) ? $"Admin: {email}" : (name ?? "Authenticated User");
    }
}
