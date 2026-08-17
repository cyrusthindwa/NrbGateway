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

public enum NrbEnvironment
{
    TEST,
    PRODUCTION
}

public enum FreshnessUnit
{
    HOURS,
    DAYS,
    MONTHS
}

public enum DataType
{
    BIOGRAPHIC_RECORD,
    VERIFICATION_EVENT
}

public enum SettingArea
{
    SUBSIDIARY_KEY,
    RATE_LIMIT,
    TIER_TOGGLE,
    NRB_ENVIRONMENT,
    CACHE_POLICY,
    AUDIT_RETENTION,
    ADMIN_USER
}
