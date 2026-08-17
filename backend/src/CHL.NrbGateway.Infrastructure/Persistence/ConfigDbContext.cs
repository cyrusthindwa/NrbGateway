using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Entities.Config;
using Microsoft.EntityFrameworkCore;

namespace CHL.NrbGateway.Infrastructure.Persistence;

public class ConfigDbContext : DbContext, IConfigDbContext
{
    public ConfigDbContext(DbContextOptions<ConfigDbContext> options) : base(options) { }

    public IQueryable<AdminUser> AdminUsers => Set<AdminUser>();
    public IQueryable<Subsidiary> Subsidiaries => Set<Subsidiary>();
    public IQueryable<SubsidiaryApiKey> SubsidiaryApiKeys => Set<SubsidiaryApiKey>();
    public IQueryable<VerificationTierSetting> VerificationTierSettings => Set<VerificationTierSetting>();
    public IQueryable<NrbEnvironmentSetting> NrbEnvironmentSettings => Set<NrbEnvironmentSetting>();
    public IQueryable<CacheRetentionPolicy> CacheRetentionPolicies => Set<CacheRetentionPolicy>();
    public IQueryable<ConfigAuditLog> ConfigAuditLogs => Set<ConfigAuditLog>();

    void IConfigDbContext.Add<TEntity>(TEntity entity) where TEntity : class => base.Add(entity);
    void IConfigDbContext.Update<TEntity>(TEntity entity) where TEntity : class => base.Update(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Target schema "config"
        modelBuilder.HasDefaultSchema("config");

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("admin_users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Subsidiary>(entity =>
        {
            entity.ToTable("subsidiaries");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShortCode).IsUnique();
        });

        modelBuilder.Entity<SubsidiaryApiKey>(entity =>
        {
            entity.ToTable("subsidiary_api_keys");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.KeyHash).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(e => e.Subsidiary)
                .WithMany(s => s.ApiKeys)
                .HasForeignKey(e => e.SubsidiaryId);

            entity.HasOne(e => e.CreatedByAdmin)
                .WithMany(a => a.CreatedApiKeys)
                .HasForeignKey(e => e.CreatedBy);
        });

        modelBuilder.Entity<VerificationTierSetting>(entity =>
        {
            entity.ToTable("verification_tier_settings");
            entity.HasKey(e => e.Tier);
            entity.Property(e => e.Tier).HasConversion<string>();

            entity.HasOne(e => e.UpdatedByAdmin)
                .WithMany(a => a.UpdatedTierSettings)
                .HasForeignKey(e => e.UpdatedBy);
        });

        modelBuilder.Entity<NrbEnvironmentSetting>(entity =>
        {
            entity.ToTable("nrb_environment_settings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Environment).HasConversion<string>();

            entity.HasOne(e => e.UpdatedByAdmin)
                .WithMany(a => a.UpdatedEnvSettings)
                .HasForeignKey(e => e.UpdatedBy);
        });

        modelBuilder.Entity<CacheRetentionPolicy>(entity =>
        {
            entity.ToTable("cache_retention_policy");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DataType).HasConversion<string>();
            entity.Property(e => e.FreshnessUnit).HasConversion<string>();

            entity.HasOne(e => e.UpdatedByAdmin)
                .WithMany(a => a.UpdatedCachePolicies)
                .HasForeignKey(e => e.UpdatedBy);
        });

        modelBuilder.Entity<ConfigAuditLog>(entity =>
        {
            entity.ToTable("config_audit_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SettingArea).HasConversion<string>();

            entity.HasOne(e => e.AdminUser)
                .WithMany(a => a.AuditLogs)
                .HasForeignKey(e => e.AdminId);

            entity.HasOne(e => e.RollbackOfEntry)
                .WithMany()
                .HasForeignKey(e => e.RollbackOfId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
