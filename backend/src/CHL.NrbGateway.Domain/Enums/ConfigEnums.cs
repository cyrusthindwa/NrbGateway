namespace CHL.NrbGateway.Domain.Enums;

public enum AdminStatus
{
    ACTIVE,
    DISABLED
}

public enum ApiKeyStatus
{
    ACTIVE,
    REVOKED
}

public enum ApiKeyEnvironment
{
    TEST,
    LIVE
}

public enum NrbEnvironment
{
    TEST,
    PRODUCTION
}

public enum SettingArea
{
    PROJECT_KEY,
    RATE_LIMIT,
    TIER_TOGGLE,
    NRB_ENVIRONMENT,
    AUDIT_RETENTION,
    ADMIN_USER,
    COMPANY,
    PROJECT,
    NOTIFICATION_CHANNEL,
    MANUAL_PORTAL_USER,
    CORS_ORIGIN
}

public enum BillingInvoiceStatus
{
    PENDING,
    INVOICED,
    PAID
}

public enum IncidentDetectionMethod
{
    AUTOMATIC,
    MANUAL
}

public enum NotificationChannelType
{
    EMAIL,
    SMS,
    WEBHOOK
}

public enum RevalidationTriggerType
{
    MANUAL,
    SCHEDULED
}
