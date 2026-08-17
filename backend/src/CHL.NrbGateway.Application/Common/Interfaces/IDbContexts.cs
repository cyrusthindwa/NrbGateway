using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Entities.Kyc;

namespace CHL.NrbGateway.Application.Common.Interfaces;

public interface IKycDbContext
{
    IQueryable<Individual> Individuals { get; }
    IQueryable<IndividualIdentification> IndividualIdentifications { get; }
    IQueryable<IndividualFieldVerification> IndividualFieldVerifications { get; }

    IQueryable<NrbVerificationEvent> NrbVerificationEvents { get; }
    IQueryable<GatewayRequest> GatewayRequests { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IConfigDbContext
{
    IQueryable<AdminUser> AdminUsers { get; }
    IQueryable<Subsidiary> Subsidiaries { get; }
    IQueryable<SubsidiaryApiKey> SubsidiaryApiKeys { get; }
    IQueryable<VerificationTierSetting> VerificationTierSettings { get; }
    IQueryable<NrbEnvironmentSetting> NrbEnvironmentSettings { get; }
    IQueryable<CacheRetentionPolicy> CacheRetentionPolicies { get; }
    IQueryable<ConfigAuditLog> ConfigAuditLogs { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
