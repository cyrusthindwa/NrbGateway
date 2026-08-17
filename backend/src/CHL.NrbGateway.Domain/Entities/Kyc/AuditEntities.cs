using CHL.NrbGateway.Domain.Enums;

namespace CHL.NrbGateway.Domain.Entities.Kyc;

public class NrbVerificationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? IndividualId { get; set; }
    public string PinSubmittedHash { get; set; } = default!;
    public NrbTier Tier { get; set; }
    public string RequestingSubsidiary { get; set; } = default!;
    public DateTimeOffset RequestTimestamp { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ResponseTimestamp { get; set; } = DateTimeOffset.UtcNow;
    public string ResponseStatus { get; set; } = default!;
    public string? ConfirmationToken { get; set; }
    public string? RawResponseRef { get; set; }

    public Individual? Individual { get; set; }
    public ICollection<GatewayRequest> GatewayRequests { get; set; } = new List<GatewayRequest>();
}

public class GatewayRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubsidiaryId { get; set; } // Reference to Subsidiary in Config schema
    public Guid? IndividualId { get; set; }
    public ServedFrom ServedFrom { get; set; }
    public Guid? NrbVerificationEventId { get; set; }
    public string ResponseStatus { get; set; } = default!;
    public DateTimeOffset RequestTimestamp { get; set; } = DateTimeOffset.UtcNow;

    public Individual? Individual { get; set; }
    public NrbVerificationEvent? NrbVerificationEvent { get; set; }
}
