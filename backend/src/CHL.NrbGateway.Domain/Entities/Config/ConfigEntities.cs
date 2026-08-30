using System.ComponentModel.DataAnnotations;
using CHL.NrbGateway.Domain.Enums;

namespace CHL.NrbGateway.Domain.Entities.Config;

public class AdminUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public AdminStatus Status { get; set; } = AdminStatus.ACTIVE;
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? PasswordResetTokenHash { get; set; }
    public DateTimeOffset? PasswordResetExpiresAt { get; set; }

    public ICollection<ProjectApiKey> CreatedApiKeys { get; set; } = new List<ProjectApiKey>();
    public ICollection<VerificationTierSetting> UpdatedTierSettings { get; set; } = new List<VerificationTierSetting>();
    public ICollection<NrbEnvironmentSetting> UpdatedEnvSettings { get; set; } = new List<NrbEnvironmentSetting>();
    public ICollection<ConfigAuditLog> AuditLogs { get; set; } = new List<ConfigAuditLog>();
    public ICollection<NotificationChannel> CreatedNotificationChannels { get; set; } = new List<NotificationChannel>();
    public ICollection<AdminOtpCode> OtpCodes { get; set; } = new List<AdminOtpCode>();
}

/// <summary>One-time passcode issued during admin two-factor login.</summary>
public class AdminOtpCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdminId { get; set; }
    public string CodeHash { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Used { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public AdminUser Admin { get; set; } = default!;
}

/// <summary>Top-level entity (e.g. CDH Investment Bank).</summary>
public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string ShortCode { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

/// <summary>A company can have multiple projects (separate integrations).</summary>
public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = default!;
    public string ShortCode { get; set; } = default!;
    public string ProjectType { get; set; } = "SYSTEM_INTEGRATION"; // SYSTEM_INTEGRATION | MANUAL_PORTAL
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Company Company { get; set; } = default!;
    public ICollection<ProjectApiKey> ApiKeys { get; set; } = new List<ProjectApiKey>();
}

public class ProjectApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string KeyHash { get; set; } = default!;
    public string KeyPrefix { get; set; } = default!;
    public ApiKeyStatus Status { get; set; } = ApiKeyStatus.ACTIVE;
    public int RateLimitPerMinute { get; set; } = 100;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RotatedAtRevokedAt { get; set; }
    public Guid CreatedBy { get; set; }

    public Project Project { get; set; } = default!;
    public AdminUser CreatedByAdmin { get; set; } = default!;
}

public class VerificationTierSetting
{
    [Key]
    public NrbTier Tier { get; set; } // Primary Key
    public bool Enabled { get; set; }
    public decimal CostPerRequest { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid UpdatedBy { get; set; }

    public AdminUser UpdatedByAdmin { get; set; } = default!;
}

public class NrbEnvironmentSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public NrbEnvironment Environment { get; set; } = NrbEnvironment.TEST;
    public string BasicEndpointUrl { get; set; } = default!;
    public string TextLookupEndpointUrl { get; set; } = default!;
    public string IntermediateEndpointUrl { get; set; } = default!;
    public string AdvancedEndpointUrl { get; set; } = default!;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid UpdatedBy { get; set; }

    public AdminUser UpdatedByAdmin { get; set; } = default!;
}

public class ConfigAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdminId { get; set; }
    public SettingArea SettingArea { get; set; }
    public string SettingKey { get; set; } = default!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? RollbackOfId { get; set; }

    public AdminUser AdminUser { get; set; } = default!;
    public ConfigAuditLog? RollbackOfEntry { get; set; }
}

public class RevalidationBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RevalidationTriggerType TriggerType { get; set; }
    public Guid? InitiatedBy { get; set; } // null for scheduled runs
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public int TotalCount { get; set; }
    public int ValidCount { get; set; }
    public int ExpiredCount { get; set; }
    public int DeceasedCount { get; set; }
    public int SeeNrbCount { get; set; }
    public int ErrorCount { get; set; }

    public AdminUser? Initiator { get; set; }
}

public class MonthlyUsageReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid CompanyId { get; set; } // denormalized from project for query convenience
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; } // 1-12
    public int RequestCount { get; set; }
    public decimal TotalCost { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class BillingInvoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal TotalAmount { get; set; }
    public BillingInvoiceStatus Status { get; set; } = BillingInvoiceStatus.PENDING;
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
    public Guid? GeneratedBy { get; set; }

    public Company Company { get; set; } = default!;
    public AdminUser? GeneratedByAdmin { get; set; }
}

public class NrbHealthCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsUp { get; set; }
    public int? LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
}

public class NrbDowntimeIncident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; } // null while ongoing
    public IncidentDetectionMethod DetectedBy { get; set; }
    public bool Notified { get; set; }
    public Guid? ResolvedBy { get; set; }

    public AdminUser? ResolvedByAdmin { get; set; }
}

public class NotificationChannel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public NotificationChannelType ChannelType { get; set; }
    public string Target { get; set; } = default!;
    public bool Enabled { get; set; } = true;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public AdminUser CreatedByAdmin { get; set; } = default!;
}
