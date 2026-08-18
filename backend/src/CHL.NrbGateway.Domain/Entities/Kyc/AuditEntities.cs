using CHL.NrbGateway.Domain.Enums;

namespace CHL.NrbGateway.Domain.Entities.Kyc;

public class NrbVerificationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? SubjectId { get; set; }
    public string PinSubmittedHash { get; set; } = default!;
    public string? PinSubmittedEncrypted { get; set; }
    public NrbTier Tier { get; set; }
    public string RequestingProjectCode { get; set; } = default!;
    public ResponseMode ResponseMode { get; set; }
    public TriggerSource TriggerSource { get; set; }
    public DateTimeOffset RequestTimestamp { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ResponseTimestamp { get; set; } = DateTimeOffset.UtcNow;
    public string ResponseStatus { get; set; } = default!;
    public string? ConfirmationToken { get; set; }
    public string? RawResponseRef { get; set; }
    public Guid? RevalidationBatchId { get; set; } // bare Guid, no cross-context navigation

    public IdentityLookup? Subject { get; set; }
    public ICollection<GatewayRequest> GatewayRequests { get; set; } = new List<GatewayRequest>();
}

public class GatewayRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; } // Reference to Project in Config schema — bare Guid, no navigation
    public Guid? SubjectId { get; set; }
    public ServedFrom ServedFrom { get; set; }
    public Guid? NrbVerificationEventId { get; set; }
    public string ResponseStatus { get; set; } = default!;
    public decimal? CostIncurred { get; set; } // snapshotted at request time; NRB-served only
    public DateTimeOffset RequestTimestamp { get; set; } = DateTimeOffset.UtcNow;

    public IdentityLookup? Subject { get; set; }
    public NrbVerificationEvent? NrbVerificationEvent { get; set; }
}
