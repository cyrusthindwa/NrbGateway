using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Entities.Kyc;

namespace CHL.NrbGateway.Domain.Entities.ManualPortal;

public class ManualUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string Status { get; set; } = "ACTIVE"; // ACTIVE | DISABLED
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
    public string? PasswordResetTokenHash { get; set; }
    public DateTimeOffset? PasswordResetExpiresAt { get; set; }

    public Company Company { get; set; } = default!;
    public ICollection<ManualVerificationLog> VerificationLogs { get; set; } = new List<ManualVerificationLog>();
}

public class ManualVerificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ManualUserId { get; set; }
    public Guid CompanyId { get; set; }
    public string NationalIdMasked { get; set; } = default!;
    public string ResultStatus { get; set; } = default!;
    public Guid? GatewayRequestId { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

    public ManualUser ManualUser { get; set; } = default!;
    public Company Company { get; set; } = default!;
}

public class ManualUserOtpCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ManualUserId { get; set; }
    public string CodeHash { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Used { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ManualUser ManualUser { get; set; } = default!;
}
