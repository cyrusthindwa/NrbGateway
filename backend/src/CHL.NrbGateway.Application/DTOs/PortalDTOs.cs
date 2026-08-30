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

public record UpdateCompanyDto(
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
    string? BasicEndpointUrl,
    string? TextLookupEndpointUrl,
    string? IntermediateEndpointUrl,
    string? AdvancedEndpointUrl
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

public record OtpChallengeDto(
    Guid AdminId,
    int ExpiresInSeconds,
    string Message
);

public record VerifyOtpDto(
    [Required] Guid AdminId,
    [Required] string Code
);

public record ResendOtpDto(
    [Required] Guid AdminId
);

public record CreateAdminUserDto(
    [Required] string Name,
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password
);

public record UpdateAdminUserDto(
    [Required] string Name,
    [Required][EmailAddress] string Email
);

public record UpdateAdminStatusDto(
    [Required] AdminStatus Status
);

public record ResetPasswordRequestDto(
    [Required] Guid AdminId,
    [Required] string Token,
    [Required][MinLength(8)] string NewPassword
);

public record DashboardMetricsDto(
    int ActiveProjects,
    int ActiveProjectsChange,
    int RequestsToday,
    int RequestsTodayChange,
    double? CacheHitRate,
    double CacheHitRateTarget,
    string NrbLinkStatus,
    int? NrbLinkLatency,
    DateTimeOffset? NrbLastCheckedAt
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

public record ManualPortalUserDto(
    Guid Id,
    string Email,
    Guid CompanyId,
    string CompanyName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt
);

public record CreateManualPortalUserDto(
    [Required][EmailAddress] string Email,
    [Required] Guid CompanyId,
    [Required][MinLength(8)] string Password
);

public record UpdateManualPortalUserStatusDto(
    [Required] string Status
);

public record DailyUsageDto(
    string Day,
    int Requests
);

public record UpdateRateLimitDto(
    [Range(1, 1000000)] int RateLimitPerMinute
);

public record NotificationChannelDto(
    Guid Id,
    NotificationChannelType ChannelType,
    string Target,
    bool Enabled,
    Guid CreatedBy,
    DateTimeOffset CreatedAt
);

public record CreateNotificationChannelDto(
    [Required] NotificationChannelType ChannelType,
    [Required] string Target
);

public record UpdateNotificationChannelDto(
    [Required] bool Enabled
);

public record PaginatedResponseDto<T>(
    IEnumerable<T> Data,
    int Total,
    int Page,
    int PageSize,
    int TotalPages
);

public record RevalidationBatchDto(
    Guid Id,
    RevalidationTriggerType TriggerType,
    Guid? InitiatedBy,
    string? InitiatedByName,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int TotalCount,
    int ValidCount,
    int ExpiredCount,
    int DeceasedCount,
    int SeeNrbCount,
    int ErrorCount
);

public record NrbStatusDto(
    string Status,
    bool? IsUp,
    int? LatencyMs,
    string? ErrorMessage,
    DateTimeOffset? LastCheckedAt,
    NrbDowntimeIncidentDto? OpenIncident
);

public record NrbDowntimeIncidentDto(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string DetectedBy,
    bool Notified,
    Guid? ResolvedBy,
    string? ResolvedByName
);

public record BillingTodayDto(
    Guid CompanyId,
    string CompanyName,
    string CompanyShortCode,
    decimal CompanyTotalCost,
    int CompanyTotalRequests,
    IReadOnlyList<ProjectUsageTodayDto> Projects
);

public record ProjectUsageTodayDto(
    Guid ProjectId,
    string ProjectName,
    string ProjectShortCode,
    decimal TotalCost,
    int TotalRequests
);

public record MonthlyUsageReportDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string ProjectShortCode,
    Guid CompanyId,
    string CompanyName,
    int PeriodYear,
    int PeriodMonth,
    int RequestCount,
    decimal TotalCost,
    DateTimeOffset GeneratedAt
);

public record BillingInvoiceDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string CompanyShortCode,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal TotalAmount,
    BillingInvoiceStatus Status,
    DateTimeOffset GeneratedAt,
    DateTimeOffset? PaidAt
);

public record GenerateInvoiceDto(
    [Required] Guid CompanyId,
    [Required][Range(2000, 2100)] int PeriodYear,
    [Required][Range(1, 12)] int PeriodMonth
);

public record GenerateReportsDto(
    [Required][Range(2000, 2100)] int PeriodYear,
    [Required][Range(1, 12)] int PeriodMonth
);
