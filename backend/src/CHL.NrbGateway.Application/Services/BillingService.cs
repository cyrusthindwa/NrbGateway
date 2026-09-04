using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;

namespace CHL.NrbGateway.Application.Services;

public class BillingService : IBillingService
{
    private readonly IKycDbContext _kycDbContext;
    private readonly IConfigDbContext _configDbContext;

    public BillingService(IKycDbContext kycDbContext, IConfigDbContext configDbContext)
    {
        _kycDbContext = kycDbContext;
        _configDbContext = configDbContext;
    }

    public Task<IReadOnlyList<BillingTodayDto>> GetTodayUsageAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var today = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);

        // Only track cache misses (requests served from NRB) for billing, not internal database cache hits
        var requests = _kycDbContext.GatewayRequests
            .Where(r => r.RequestTimestamp >= today && r.ServedFrom == ServedFrom.NRB)
            .ToList();

        var projects = _configDbContext.Projects.ToList();
        var companies = _configDbContext.Companies.ToList();

        var grouped = requests
            .GroupBy(r => r.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                TotalCost = g.Sum(r => r.CostIncurred ?? 0m),
                TotalRequests = g.Count()
            })
            .ToList();

        var result = new List<BillingTodayDto>();

        foreach (var company in companies)
        {
            var companyProjects = projects.Where(p => p.CompanyId == company.Id).ToList();
            var projectUsages = companyProjects
                .Select(p =>
                {
                    var usage = grouped.FirstOrDefault(g => g.ProjectId == p.Id);
                    return new ProjectUsageTodayDto(
                        p.Id, p.Name, p.ShortCode,
                        usage?.TotalCost ?? 0m,
                        usage?.TotalRequests ?? 0);
                })
                .ToList();

            result.Add(new BillingTodayDto(
                company.Id, company.Name, company.ShortCode,
                projectUsages.Sum(u => u.TotalCost),
                projectUsages.Sum(u => u.TotalRequests),
                projectUsages));
        }

        return Task.FromResult<IReadOnlyList<BillingTodayDto>>(result);
    }

    public async Task GenerateMonthlyReportsAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);

        // Only track cache misses (requests served from NRB) for billing, not internal database cache hits
        var requests = _kycDbContext.GatewayRequests
            .Where(r => r.RequestTimestamp >= start && r.RequestTimestamp < end && r.ServedFrom == ServedFrom.NRB)
            .ToList();

        var projects = _configDbContext.Projects.ToList();

        foreach (var project in projects)
        {
            var projectRequests = requests.Where(r => r.ProjectId == project.Id).ToList();
            var count = projectRequests.Count;
            var cost = projectRequests.Sum(r => r.CostIncurred ?? 0m);

            var existing = _configDbContext.MonthlyUsageReports
                .FirstOrDefault(m => m.ProjectId == project.Id && m.PeriodYear == year && m.PeriodMonth == month);

            if (existing == null)
            {
                _configDbContext.Add(new MonthlyUsageReport
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    CompanyId = project.CompanyId,
                    PeriodYear = year,
                    PeriodMonth = month,
                    RequestCount = count,
                    TotalCost = cost,
                    GeneratedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                existing.RequestCount = count;
                existing.TotalCost = cost;
                existing.GeneratedAt = DateTimeOffset.UtcNow;
                _configDbContext.Update(existing);
            }
        }

        await _configDbContext.SaveChangesAsync(cancellationToken);
    }
}
