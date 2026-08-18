using System.ComponentModel.DataAnnotations;
using CHL.NrbGateway.Domain.Enums;

namespace CHL.NrbGateway.Application.DTOs;

public record CompanyDto(
    Guid Id,
    string Name,
    string ShortCode,
    DateTimeOffset CreatedAt
);

public record CreateCompanyDto(
    [Required] string Name,
    [Required] string ShortCode
);

public record ProjectDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string ShortCode,
    DateTimeOffset CreatedAt
);

public record CreateProjectDto(
    [Required] Guid CompanyId,
    [Required] string Name,
    [Required] string ShortCode
);

public record ApiKeyResponseDto(
    Guid Id,
    Guid ProjectId,
    string PlaintextApiKey,
    string KeyPrefix,
    ApiKeyStatus Status,
    int RateLimitPerMinute,
    DateTimeOffset CreatedAt
);

public record ProjectApiKeySummaryDto(
    Guid Id,
    Guid ProjectId,
    string KeyPrefix,
    ApiKeyStatus Status,
    int RateLimitPerMinute,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RotatedAtRevokedAt
);

public record TierSettingDto(
    NrbTier Tier,
    bool Enabled,
    decimal CostPerRequest,
    DateTimeOffset UpdatedAt,
    Guid UpdatedBy
);

public record UpdateTierSettingDto(
    bool Enabled,
    decimal? CostPerRequest
);

public record EnvironmentSettingDto(
    Guid Id,
    NrbEnvironment Environment,
    string BasicEndpointUrl,
    string TextLookupEndpointUrl,
    string IntermediateEndpointUrl,
    string AdvancedEndpointUrl,
    DateTimeOffset UpdatedAt,
    Guid UpdatedBy
);

public record UpdateEnvironmentSettingDto(
    NrbEnvironment Environment,
    string BasicEndpointUrl,
    string TextLookupEndpointUrl,
    string IntermediateEndpointUrl,
    string AdvancedEndpointUrl
);

public record AdminLoginDto(
    [Required][EmailAddress] string Email,
    [Required] string Password
);

public record AdminLoginResponseDto(
    string Token,
    Guid AdminId,
    string Name,
    string Email
);

public record DashboardMetricsDto(
    int ActiveProjects,
    int ActiveProjectsChange,
    int RequestsToday,
    int RequestsTodayChange,
    double CacheHitRate,
    double CacheHitRateTarget,
    string NrbLinkStatus,
    int NrbLinkLatency
);

public record RecentChangeDto(
    Guid Id,
    string Admin,
    string ChangeDetails,
    DateTimeOffset Timestamp
);

public record AuditLogEntryDto(
    Guid Id,
    DateTimeOffset Timestamp,
    string Admin,
    string SettingChanged,
    string OldValue,
    string NewValue,
    string ActionType
);

public record AdminUserDto(
    Guid Id,
    string Name,
    string Email,
    string Status,
    DateTimeOffset CreatedAt
);

public record DailyUsageDto(
    string Day,
    int Requests
);

public record PaginatedResponseDto<T>(
    IEnumerable<T> Data,
    int Total,
    int Page,
    int PageSize,
    int TotalPages
);
