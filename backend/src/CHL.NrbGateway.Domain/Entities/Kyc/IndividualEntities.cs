using CHL.NrbGateway.Domain.Enums;

namespace CHL.NrbGateway.Domain.Entities.Kyc;

/// <summary>
/// Pseudonymization boundary — the ONLY table that holds the real national ID
/// number (HMAC-SHA256 hash for lookup + AES-encrypted ciphertext, decrypted
/// only when the raw PIN is genuinely needed). Every other kyc entity keys off
/// subject_id, never off the PIN.
/// </summary>
public class IdentityLookup
{
    public Guid SubjectId { get; set; } = Guid.NewGuid();
    public string NationalIdHash { get; set; } = default!;
    public string NationalIdEncrypted { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Individual? Individual { get; set; }
    public ICollection<NrbVerificationEvent> VerificationEvents { get; set; } = new List<NrbVerificationEvent>();
    public ICollection<NrbFieldCheckResult> FieldCheckResults { get; set; } = new List<NrbFieldCheckResult>();
    public ICollection<GatewayRequest> GatewayRequests { get; set; } = new List<GatewayRequest>();
}

/// <summary>
/// NRB registry mirror — keyed by subject_id (FK to identity_lookup), never by
/// the PIN directly. Stores only data provided by the National Registration
/// Bureau.
/// </summary>
public class Individual
{
    public Guid SubjectId { get; set; }

    // ── Biographic data ──
    public string? Surname { get; set; }
    public string? FirstName { get; set; }
    public string? OtherNames { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }          // Basic only
    public string? CivilStatus { get; set; }          // Text Lookup only
    public string? BirthDistrict { get; set; }        // Basic / Text Lookup only
    public string? ResidenceAddress { get; set; }     // Text Lookup "address" or Advanced "PlaceOfPermanentResidence"
    public string? NrbRegisteredPhone { get; set; }   // Text Lookup only, reference only
    public DateOnly? IdDateOfIssue { get; set; }
    public DateOnly? IdDateOfExpiry { get; set; }

    // ── Two genuinely separate NRB status vocabularies ──
    public string CardStatus { get; set; } = string.Empty;   // VALID | EXPIRED | RENEWAL PROCESSED | PERSON DECEASED | SEE NRB | INVALID | NOT FOUND
    public string? MiddlewareStatus { get; set; }            // BLACKLISTED | DECEASED | NOT_ACCEPTED | DUPLICATE | REMOVED | UNDER_DURESS | UNKNOWN | CLEAR

    public DateTimeOffset? LastCardCheckAt { get; set; }
    public DateTimeOffset? LastMiddlewareCheckAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // ── Navigation ──
    public IdentityLookup IdentityLookup { get; set; } = default!;
    public ICollection<IndividualSourceValue> SourceValues { get; set; } = new List<IndividualSourceValue>();
    public ICollection<IndividualDocument> Documents { get; set; } = new List<IndividualDocument>();
}

/// <summary>
/// Field-level provenance — one row every time any tier reports a value for a
/// field, so nothing is silently overwritten when two tiers disagree.
/// </summary>
public class IndividualSourceValue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubjectId { get; set; }
    public string FieldName { get; set; } = default!;
    public string Value { get; set; } = default!;
    public FieldSource Source { get; set; }
    public DateTimeOffset ObservedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsCurrent { get; set; }

    public Individual Individual { get; set; } = default!;
}

/// <summary>
/// One row per document (photo, fingerprint, signature) rather than fixed
/// columns. blob_ref points into object storage (MinIO).
/// </summary>
public class IndividualDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubjectId { get; set; }
    public DocumentType DocumentType { get; set; }
    public DocumentSource Source { get; set; }
    public string? BlobFormat { get; set; }   // e.g. WSQ, JPG; null when the source doesn't declare it
    public string? FingerPosition { get; set; }
    public string BlobRef { get; set; } = default!;
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    public Individual Individual { get; set; } = default!;
}

/// <summary>
/// Basic-tier per-field check results (CORRECT / INCORRECT). SubjectId is
/// nullable when the PIN had not yet been resolved at check time.
/// </summary>
public class NrbFieldCheckResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? SubjectId { get; set; }
    public string FieldName { get; set; } = default!;
    public string Result { get; set; } = default!;   // CORRECT | INCORRECT
    public NrbTier Tier { get; set; }
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;

    public IdentityLookup? Subject { get; set; }
}
