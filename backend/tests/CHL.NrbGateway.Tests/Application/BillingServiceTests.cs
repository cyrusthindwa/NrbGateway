using CHL.NrbGateway.Application.Services;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Entities.Kyc;
using CHL.NrbGateway.Domain.Enums;
using CHL.NrbGateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CHL.NrbGateway.Tests.Application;

public class BillingServiceTests
{
    private static (KycDbContext, ConfigDbContext) BuildInMemoryContexts()
    {
        var kycOpts = new DbContextOptionsBuilder<KycDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var configOpts = new DbContextOptionsBuilder<ConfigDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return (new KycDbContext(kycOpts), new ConfigDbContext(configOpts));
    }

    [Fact]
    public async Task GetTodayUsage_OnlyTracksCacheMisses_ExcludesInternalCacheHits()
    {
        var (kycDb, configDb) = BuildInMemoryContexts();

        var companyId = Guid.NewGuid();
        var company = new Company
        {
            Id = companyId,
            Name = "CDH Investment Bank",
            ShortCode = "CDHIB",
            CreatedAt = DateTimeOffset.UtcNow
        };
        configDb.Add(company);

        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            CompanyId = companyId,
            Name = "KYC Portal",
            ShortCode = "CDH-KYC",
            ProjectType = "SYSTEM_INTEGRATION",
            CreatedAt = DateTimeOffset.UtcNow
        };
        configDb.Add(project);
        configDb.SaveChanges();

        // 1 NRB request (cache miss) with cost
        kycDb.Add(new GatewayRequest
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ServedFrom = ServedFrom.NRB,
            CostIncurred = 50m,
            ResponseStatus = "IDENTITY_VERIFIED",
            RequestTimestamp = DateTimeOffset.UtcNow
        });

        // 4 Cache hits (internal DB) with null cost
        for (int i = 0; i < 4; i++)
        {
            kycDb.Add(new GatewayRequest
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                ServedFrom = ServedFrom.CACHE,
                CostIncurred = null,
                ResponseStatus = "IDENTITY_VERIFIED",
                RequestTimestamp = DateTimeOffset.UtcNow
            });
        }
        kycDb.SaveChanges();

        var billingSvc = new BillingService(kycDb, configDb);
        var usage = await billingSvc.GetTodayUsageAsync();

        Assert.Single(usage);
        var companyUsage = usage[0];
        Assert.Equal(companyId, companyUsage.CompanyId);

        // Crucial assertions: Only 1 request tracked in billing (the cache miss to NRB), NOT 5 (1 + 4 cache hits)
        Assert.Equal(1, companyUsage.CompanyTotalRequests);
        Assert.Equal(50m, companyUsage.CompanyTotalCost);

        Assert.Single(companyUsage.Projects);
        var projectUsage = companyUsage.Projects[0];
        Assert.Equal(1, projectUsage.TotalRequests);
        Assert.Equal(50m, projectUsage.TotalCost);
    }

    [Fact]
    public async Task GenerateMonthlyReports_OnlyTracksCacheMisses()
    {
        var (kycDb, configDb) = BuildInMemoryContexts();

        var companyId = Guid.NewGuid();
        configDb.Add(new Company
        {
            Id = companyId,
            Name = "CDH Investment Bank",
            ShortCode = "CDHIB",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var projectId = Guid.NewGuid();
        configDb.Add(new Project
        {
            Id = projectId,
            CompanyId = companyId,
            Name = "KYC Portal",
            ShortCode = "CDH-KYC",
            ProjectType = "SYSTEM_INTEGRATION",
            CreatedAt = DateTimeOffset.UtcNow
        });
        configDb.SaveChanges();

        var now = DateTimeOffset.UtcNow;
        // 2 NRB requests in current month
        kycDb.Add(new GatewayRequest
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ServedFrom = ServedFrom.NRB,
            CostIncurred = 25m,
            ResponseStatus = "IDENTITY_VERIFIED",
            RequestTimestamp = now
        });
        kycDb.Add(new GatewayRequest
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ServedFrom = ServedFrom.NRB,
            CostIncurred = 25m,
            ResponseStatus = "IDENTITY_VERIFIED",
            RequestTimestamp = now
        });

        // 10 Cache hits in current month
        for (int i = 0; i < 10; i++)
        {
            kycDb.Add(new GatewayRequest
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                ServedFrom = ServedFrom.CACHE,
                CostIncurred = null,
                ResponseStatus = "IDENTITY_VERIFIED",
                RequestTimestamp = now
            });
        }
        kycDb.SaveChanges();

        var billingSvc = new BillingService(kycDb, configDb);
        await billingSvc.GenerateMonthlyReportsAsync(now.Year, now.Month);

        var report = await configDb.MonthlyUsageReports.FirstOrDefaultAsync(r => r.ProjectId == projectId);
        Assert.NotNull(report);
        Assert.Equal(2, report.RequestCount); // Only the 2 NRB cache misses, ignoring 10 cache hits
        Assert.Equal(50m, report.TotalCost);
    }
}
