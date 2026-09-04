namespace CHL.NrbGateway.Application.Common.Interfaces;

public record SystemErrorContext(
    string? RequestPath = null,
    string? HttpMethod = null,
    string? QueryString = null,
    string? ClientIp = null,
    string? UserAgent = null,
    string? UserOrProject = null,
    int? StatusCode = null
);

public interface IErrorNotifierService
{
    Task NotifyErrorAsync(Exception exception, SystemErrorContext? context = null, CancellationToken cancellationToken = default);
}
