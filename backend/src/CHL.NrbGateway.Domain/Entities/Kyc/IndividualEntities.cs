using CHL.NrbGateway.Domain.Enums;

namespace CHL.NrbGateway.Domain.Entities.Kyc;

/// <summary>
/// NRB registry mirror — stores only data provided by the National Registration Bureau.
/// Corporate KYC data (addresses, contacts, employment, next of kin) is the
/// responsibility of each subsidiary's own system, not this gateway.
/// </summary>
public class Individual
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Identification ──
    public string? NationalIdHash { get; set; }         // HMAC-SHA256 for indexed lookups
    public string? NationalIdEncrypted { get; set; }     // AES-256 encrypted PIN

    // ── NRB Data section ──
    public string Surname { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string? OtherNames { get; set; }
    public Gender Gender { get; set; }
    public string? MaritalStatus { get; set; }           // Marital status at time of NID registration
    public string? BirthDistrict { get; set; }
    public string? ResidentialAddress { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? TelephoneNumber { get; set; }
    public string CardStatus { get; set; } = default!;   // VALID | EXPIRED | PERSON DECEASED | SEE NRB | ...

    // ── NRB Document section (stored as blob references) ──
    public string? PhotoRef { get; set; }
    public string? FingerprintRef { get; set; }
    public string? FingerPosition { get; set; }

    // ── Mirror status ──
    public RecordStatus RecordStatus { get; set; } = RecordStatus.UNVERIFIED;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastRevalidatedAt { get; set; }

    // ── Navigation ──
    public ICollection<IndividualIdentification> Identifications { get; set; } = new List<IndividualIdentification>();
    public ICollection<IndividualFieldVerification> FieldVerifications { get; set; } = new List<IndividualFieldVerification>();
    public ICollection<NrbVerificationEvent> VerificationEvents { get; set; } = new List<NrbVerificationEvent>();
}

public class IndividualIdentification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IndividualId { get; set; }
    public IdType IdType { get; set; }
    public string IdValue { get; set; } = default!;
    public string? IssuingAuthority { get; set; }
    public string? IdStatus { get; set; }
    public DateOnly? DateOfIssue { get; set; }
    public DateOnly? DateOfExpiry { get; set; }

    public Individual Individual { get; set; } = default!;
}

public class IndividualFieldVerification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IndividualId { get; set; }
    public string FieldName { get; set; } = default!;
    public string Value { get; set; } = default!;
    public VerificationSource Source { get; set; }
    public VerificationFieldStatus VerificationStatus { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public bool Superseded { get; set; }

    public Individual Individual { get; set; } = default!;
}
