using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Entities.Kyc;

namespace CHL.NrbGateway.Application.Common.Interfaces;

public interface IKycDbContext
{
    IQueryable<IdentityLookup> IdentityLookups { get; }
    IQueryable<Individual> Individuals { get; }
    IQueryable<IndividualSourceValue> IndividualSourceValues { get; }
    IQueryable<IndividualDocument> IndividualDocuments { get; }
    IQueryable<NrbFieldCheckResult> NrbFieldCheckResults { get; }

    IQueryable<NrbVerificationEvent> NrbVerificationEvents { get; }
    IQueryable<GatewayRequest> GatewayRequests { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IConfigDbContext
{
    IQueryable<AdminUser> AdminUsers { get; }
    IQueryable<Company> Companies { get; }
    IQueryable<Project> Projects { get; }
    IQueryable<ProjectApiKey> ProjectApiKeys { get; }
    IQueryable<VerificationTierSetting> VerificationTierSettings { get; }
    IQueryable<NrbEnvironmentSetting> NrbEnvironmentSettings { get; }
    IQueryable<ConfigAuditLog> ConfigAuditLogs { get; }
    IQueryable<RevalidationBatch> RevalidationBatches { get; }
    IQueryable<MonthlyUsageReport> MonthlyUsageReports { get; }
    IQueryable<BillingInvoice> BillingInvoices { get; }
    IQueryable<NrbHealthCheck> NrbHealthChecks { get; }
    IQueryable<NrbDowntimeIncident> NrbDowntimeIncidents { get; }
    IQueryable<NotificationChannel> NotificationChannels { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
