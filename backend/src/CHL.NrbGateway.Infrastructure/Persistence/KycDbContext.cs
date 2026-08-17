using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Entities.Kyc;
using Microsoft.EntityFrameworkCore;

namespace CHL.NrbGateway.Infrastructure.Persistence;

public class KycDbContext : DbContext, IKycDbContext
{
    public KycDbContext(DbContextOptions<KycDbContext> options) : base(options) { }

    // ── NRB Mirror ──
    public IQueryable<Individual> Individuals => Set<Individual>();
    public IQueryable<IndividualIdentification> IndividualIdentifications => Set<IndividualIdentification>();
    public IQueryable<IndividualFieldVerification> IndividualFieldVerifications => Set<IndividualFieldVerification>();

    // ── Verification & Audit ──
    public IQueryable<NrbVerificationEvent> NrbVerificationEvents => Set<NrbVerificationEvent>();
    public IQueryable<GatewayRequest> GatewayRequests => Set<GatewayRequest>();

    void IKycDbContext.Add<TEntity>(TEntity entity) where TEntity : class => base.Add(entity);
    void IKycDbContext.Update<TEntity>(TEntity entity) where TEntity : class => base.Update(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("kyc");

        modelBuilder.Entity<Individual>(entity =>
        {
            entity.ToTable("individuals");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NationalIdHash).IsUnique();
            entity.Property(e => e.Gender).HasConversion<string>();
            entity.Property(e => e.RecordStatus).HasConversion<string>();
        });

        modelBuilder.Entity<IndividualIdentification>(entity =>
        {
            entity.ToTable("individual_identifications");
            entity.Property(e => e.IdType).HasConversion<string>();
        });

        modelBuilder.Entity<IndividualFieldVerification>(entity =>
        {
            entity.ToTable("individual_field_verification");
            entity.Property(e => e.Source).HasConversion<string>();
            entity.Property(e => e.VerificationStatus).HasConversion<string>();
        });

        modelBuilder.Entity<NrbVerificationEvent>(entity =>
        {
            entity.ToTable("nrb_verification_events");
            entity.Property(e => e.Tier).HasConversion<string>();
            entity.HasIndex(e => e.PinSubmittedHash);
        });

        modelBuilder.Entity<GatewayRequest>(entity =>
        {
            entity.ToTable("gateway_requests");
            entity.Property(e => e.ServedFrom).HasConversion<string>();
        });
    }
}
