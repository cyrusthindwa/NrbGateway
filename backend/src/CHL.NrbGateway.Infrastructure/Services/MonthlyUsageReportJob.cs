using CHL.NrbGateway.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CHL.NrbGateway.Infrastructure.Services;

/// <summary>
/// Generates monthly usage reports for the just-closed month, running on the
/// 1st of each month shortly after midnight (00:05 UTC).
/// </summary>
public class MonthlyUsageReportJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonthlyUsageReportJob> _logger;

    public MonthlyUsageReportJob(IServiceScopeFactory scopeFactory, ILogger<MonthlyUsageReportJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextRun();
            _logger.LogInformation("Monthly usage report job: next run in {Delay}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                var now = DateTimeOffset.UtcNow;
                var prior = now.AddMonths(-1);
                using var scope = _scopeFactory.CreateScope();
                var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
                await billing.GenerateMonthlyReportsAsync(prior.Year, prior.Month, stoppingToken);
                _logger.LogInformation("Monthly usage reports generated for {Year}-{Month}.", prior.Year, prior.Month);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monthly usage report generation failed.");
            }
        }
    }

    private static TimeSpan TimeUntilNextRun()
    {
        var now = DateTimeOffset.UtcNow;
        var next = new DateTimeOffset(now.Year, now.Month, 1, 0, 5, 0, TimeSpan.Zero);
        if (next <= now)
            next = next.AddMonths(1);

        return next - now;
    }
}
