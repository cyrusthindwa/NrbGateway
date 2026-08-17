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

    public ICollection<SubsidiaryApiKey> CreatedApiKeys { get; set; } = new List<SubsidiaryApiKey>();
    public ICollection<VerificationTierSetting> UpdatedTierSettings { get; set; } = new List<VerificationTierSetting>();
    public ICollection<NrbEnvironmentSetting> UpdatedEnvSettings { get; set; } = new List<NrbEnvironmentSetting>();
    public ICollection<CacheRetentionPolicy> UpdatedCachePolicies { get; set; } = new List<CacheRetentionPolicy>();
    public ICollection<ConfigAuditLog> AuditLogs { get; set; } = new List<ConfigAuditLog>();
}

public class Subsidiary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string ShortCode { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<SubsidiaryApiKey> ApiKeys { get; set; } = new List<SubsidiaryApiKey>();
}

public class SubsidiaryApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubsidiaryId { get; set; }
    public string KeyHash { get; set; } = default!;
    public string KeyPrefix { get; set; } = default!;
    public ApiKeyStatus Status { get; set; } = ApiKeyStatus.ACTIVE;
    public int RateLimitPerMinute { get; set; } = 100;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RotatedAtRevokedAt { get; set; }
    public Guid CreatedBy { get; set; }

    public Subsidiary Subsidiary { get; set; } = default!;
    public AdminUser CreatedByAdmin { get; set; } = default!;
}

public class VerificationTierSetting
{
    public NrbTier Tier { get; set; } // Primary Key
    public bool Enabled { get; set; }
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

public class CacheRetentionPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DataType DataType { get; set; }
    public int FreshnessValue { get; set; }
    public FreshnessUnit FreshnessUnit { get; set; }
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
