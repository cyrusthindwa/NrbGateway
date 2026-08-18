using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Entities.Kyc;
using Microsoft.EntityFrameworkCore;

namespace CHL.NrbGateway.Infrastructure.Persistence;

public class KycDbContext : DbContext, IKycDbContext
{
    public KycDbContext(DbContextOptions<KycDbContext> options) : base(options) { }

    // ── Pseudonymization boundary & NRB Mirror ──
    public IQueryable<IdentityLookup> IdentityLookups => Set<IdentityLookup>();
    public IQueryable<Individual> Individuals => Set<Individual>();
    public IQueryable<IndividualSourceValue> IndividualSourceValues => Set<IndividualSourceValue>();
    public IQueryable<IndividualDocument> IndividualDocuments => Set<IndividualDocument>();
    public IQueryable<NrbFieldCheckResult> NrbFieldCheckResults => Set<NrbFieldCheckResult>();

    // ── Verification & Audit ──
    public IQueryable<NrbVerificationEvent> NrbVerificationEvents => Set<NrbVerificationEvent>();
    public IQueryable<GatewayRequest> GatewayRequests => Set<GatewayRequest>();

    void IKycDbContext.Add<TEntity>(TEntity entity) where TEntity : class => base.Add(entity);
    void IKycDbContext.Update<TEntity>(TEntity entity) where TEntity : class => base.Update(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("kyc");

        modelBuilder.Entity<IdentityLookup>(entity =>
        {
            entity.ToTable("identity_lookup");
            entity.HasKey(e => e.SubjectId);
            entity.HasIndex(e => e.NationalIdHash).IsUnique();
            entity.HasOne(e => e.Individual)
                .WithOne(i => i.IdentityLookup)
                .HasForeignKey<Individual>(i => i.SubjectId);
        });

        modelBuilder.Entity<Individual>(entity =>
        {
            entity.ToTable("individuals");
            entity.HasKey(e => e.SubjectId);
            entity.HasMany(e => e.SourceValues)
                .WithOne(s => s.Individual)
                .HasForeignKey(s => s.SubjectId);
            entity.HasMany(e => e.Documents)
                .WithOne(d => d.Individual)
                .HasForeignKey(d => d.SubjectId);
        });

        modelBuilder.Entity<IndividualSourceValue>(entity =>
        {
            entity.ToTable("individual_source_values");
            entity.Property(e => e.Source).HasConversion<string>();
        });

        modelBuilder.Entity<IndividualDocument>(entity =>
        {
            entity.ToTable("individual_documents");
            entity.Property(e => e.DocumentType).HasConversion<string>();
            entity.Property(e => e.Source).HasConversion<string>();
        });

        modelBuilder.Entity<NrbFieldCheckResult>(entity =>
        {
            entity.ToTable("nrb_field_check_results");
            entity.Property(e => e.Tier).HasConversion<string>();
            entity.HasOne(e => e.Subject)
                .WithMany(s => s.FieldCheckResults)
                .HasForeignKey(e => e.SubjectId);
        });

        modelBuilder.Entity<NrbVerificationEvent>(entity =>
        {
            entity.ToTable("nrb_verification_events");
            entity.Property(e => e.Tier).HasConversion<string>();
            entity.Property(e => e.ResponseMode).HasConversion<string>();
            entity.Property(e => e.TriggerSource).HasConversion<string>();
            entity.HasIndex(e => e.PinSubmittedHash);
            entity.HasOne(e => e.Subject)
                .WithMany(s => s.VerificationEvents)
                .HasForeignKey(e => e.SubjectId);
        });

        modelBuilder.Entity<GatewayRequest>(entity =>
        {
            entity.ToTable("gateway_requests");
            entity.Property(e => e.ServedFrom).HasConversion<string>();
            entity.HasOne(e => e.Subject)
                .WithMany(s => s.GatewayRequests)
                .HasForeignKey(e => e.SubjectId);
        });
    }
}
