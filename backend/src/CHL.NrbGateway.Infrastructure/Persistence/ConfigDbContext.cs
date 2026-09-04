using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Entities.Config;
using Microsoft.EntityFrameworkCore;

namespace CHL.NrbGateway.Infrastructure.Persistence;

public class ConfigDbContext : DbContext, IConfigDbContext
{
    public ConfigDbContext(DbContextOptions<ConfigDbContext> options) : base(options) { }

    public IQueryable<AdminUser> AdminUsers => Set<AdminUser>();
    public IQueryable<AdminOtpCode> AdminOtpCodes => Set<AdminOtpCode>();
    public IQueryable<Company> Companies => Set<Company>();
    public IQueryable<Project> Projects => Set<Project>();
    public IQueryable<ProjectApiKey> ProjectApiKeys => Set<ProjectApiKey>();
    public IQueryable<VerificationTierSetting> VerificationTierSettings => Set<VerificationTierSetting>();
    public IQueryable<NrbEnvironmentSetting> NrbEnvironmentSettings => Set<NrbEnvironmentSetting>();
    public IQueryable<ConfigAuditLog> ConfigAuditLogs => Set<ConfigAuditLog>();
    public IQueryable<RevalidationBatch> RevalidationBatches => Set<RevalidationBatch>();
    public IQueryable<MonthlyUsageReport> MonthlyUsageReports => Set<MonthlyUsageReport>();
    public IQueryable<BillingInvoice> BillingInvoices => Set<BillingInvoice>();
    public IQueryable<NrbHealthCheck> NrbHealthChecks => Set<NrbHealthCheck>();
    public IQueryable<NrbDowntimeIncident> NrbDowntimeIncidents => Set<NrbDowntimeIncident>();
    public IQueryable<NotificationChannel> NotificationChannels => Set<NotificationChannel>();
    public IQueryable<CorsOrigin> CorsOrigins => Set<CorsOrigin>();

    void IConfigDbContext.Add<TEntity>(TEntity entity) where TEntity : class => base.Add(entity);
    void IConfigDbContext.Update<TEntity>(TEntity entity) where TEntity : class => base.Update(entity);
    void IConfigDbContext.Remove<TEntity>(TEntity entity) where TEntity : class => base.Remove(entity);

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

        modelBuilder.Entity<AdminOtpCode>(entity =>
        {
            entity.ToTable("admin_otp_codes");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AdminId);

            entity.HasOne(e => e.Admin)
                .WithMany(a => a.OtpCodes)
                .HasForeignKey(e => e.AdminId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShortCode).IsUnique();
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShortCode).IsUnique();
            entity.Property(e => e.ProjectType).HasDefaultValue("SYSTEM_INTEGRATION");

            entity.HasOne(e => e.Company)
                .WithMany(c => c.Projects)
                .HasForeignKey(e => e.CompanyId);
        });

        modelBuilder.Entity<ProjectApiKey>(entity =>
        {
            entity.ToTable("project_api_keys");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.KeyHash).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(e => e.Project)
                .WithMany(p => p.ApiKeys)
                .HasForeignKey(e => e.ProjectId);

            entity.HasOne(e => e.CreatedByAdmin)
                .WithMany(a => a.CreatedApiKeys)
                .HasForeignKey(e => e.CreatedBy);
        });

        modelBuilder.Entity<VerificationTierSetting>(entity =>
        {
            entity.ToTable("verification_tier_settings");
            entity.HasKey(e => e.Tier);
            entity.Property(e => e.Tier).HasConversion<string>();
            entity.Property(e => e.CostPerRequest).HasColumnType("decimal(10,2)");

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

        modelBuilder.Entity<RevalidationBatch>(entity =>
        {
            entity.ToTable("revalidation_batches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TriggerType).HasConversion<string>();

            entity.HasOne(e => e.Initiator)
                .WithMany()
                .HasForeignKey(e => e.InitiatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MonthlyUsageReport>(entity =>
        {
            entity.ToTable("monthly_usage_reports");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProjectId, e.PeriodYear, e.PeriodMonth }).IsUnique();
            entity.Property(e => e.TotalCost).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<BillingInvoice>(entity =>
        {
            entity.ToTable("billing_invoices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId);

            entity.HasOne(e => e.GeneratedByAdmin)
                .WithMany()
                .HasForeignKey(e => e.GeneratedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NrbHealthCheck>(entity =>
        {
            entity.ToTable("nrb_health_checks");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<NrbDowntimeIncident>(entity =>
        {
            entity.ToTable("nrb_downtime_incidents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DetectedBy).HasConversion<string>();

            entity.HasOne(e => e.ResolvedByAdmin)
                .WithMany()
                .HasForeignKey(e => e.ResolvedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NotificationChannel>(entity =>
        {
            entity.ToTable("notification_channels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChannelType).HasConversion<string>();

            entity.HasOne(e => e.CreatedByAdmin)
                .WithMany(a => a.CreatedNotificationChannels)
                .HasForeignKey(e => e.CreatedBy);
        });

        modelBuilder.Entity<CorsOrigin>(entity =>
        {
            entity.ToTable("cors_origins");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Origin).IsUnique();
        });
    }
}
