using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Entities.Kyc;
using CHL.NrbGateway.Domain.Entities.ManualPortal;
using Microsoft.EntityFrameworkCore;

namespace CHL.NrbGateway.Infrastructure.Persistence;

public class ManualPortalDbContext : DbContext, IManualPortalDbContext
{
    public ManualPortalDbContext(DbContextOptions<ManualPortalDbContext> options) : base(options) { }

    public IQueryable<ManualUser> ManualUsers => Set<ManualUser>();
    public IQueryable<ManualVerificationLog> ManualVerificationLogs => Set<ManualVerificationLog>();
    public IQueryable<ManualUserOtpCode> ManualUserOtpCodes => Set<ManualUserOtpCode>();

    void IManualPortalDbContext.Add<TEntity>(TEntity entity) where TEntity : class => base.Add(entity);
    void IManualPortalDbContext.Update<TEntity>(TEntity entity) where TEntity : class => base.Update(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("verification_portal");

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies", "config");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<ManualUser>(entity =>
        {
            entity.ToTable("manual_users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId);
        });

        modelBuilder.Entity<ManualVerificationLog>(entity =>
        {
            entity.ToTable("manual_verification_log");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.ManualUser)
                .WithMany(u => u.VerificationLogs)
                .HasForeignKey(e => e.ManualUserId);

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId);
        });

        modelBuilder.Entity<ManualUserOtpCode>(entity =>
        {
            entity.ToTable("manual_user_otp_codes");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.ManualUser)
                .WithMany()
                .HasForeignKey(e => e.ManualUserId);
        });
    }
}
