namespace CHL.NrbGateway.Application.Models;

// ── Intermediate (Tier 3) — Biometric 1:1 match ──────────────────────

public record NrbIntermediateRequestModel(
    string NationalId,
    string BiometricBlob,
    string SubsidiaryCode
);

public record NrbIntermediateResponseModel(
    bool IsMatch,
    string Status,
    string? ConfirmationToken,
    string? RawResponsePayload
);

// ── Basic (Tier 1) — Full field reconciliation ───────────────────────
// Per NRB v2.0.0: submit all field values, NRB returns CORRECT/INCORRECT per field

public record NrbBasicRequestModel(
    string IdNumber,
    string Surname,
    string FirstName,
    string? OtherNames,
    string Nationality,
    string Gender,
    string DateOfBirthString,
    string? DateOfIssueString,
    string? DateOfExpiryString,
    string? PlaceOfBirthDistrictName
);

public record NrbBasicResponseModel(
    string CardStatus,
    Dictionary<string, string> FieldResults
);

public static class NrbBasicCardStatus
{
    public const string NotFound = "NOT FOUND";
    public const string Invalid = "INVALID";
    public const string Valid = "VALID";
    public const string Expired = "EXPIRED";
    public const string RenewalProcessed = "RENEWAL PROCESSED";
    public const string PersonDeceased = "PERSON DECEASED";
    public const string SeeNrb = "SEE NRB";

    public static bool IsRejected(string status) => status switch
    {
        NotFound or Invalid or PersonDeceased => true,
        _ => false
    };

    public static bool RequiresManualReview(string status) => status switch
    {
        SeeNrb or RenewalProcessed => true,
        _ => false
    };

    /// <summary>Statuses that indicate the record stored in our mirror is stale and needs re-fetching.</summary>
    public static bool IsStale(string status) => status switch
    {
        Expired or RenewalProcessed or PersonDeceased => true,
        _ => false
    };
}

// ── Text Lookup (Tier 2) — Full demographic retrieval from NRB ───────
// Matches actual NRB API response: Data + Document sections
// Auth: ClientId + ClientKey custom headers

public record NrbTextLookupRequestModel(string IdNumber);

/// <summary>
/// NRB Text Lookup response — full person profile.
/// Photograph and fingerprint blobs are persisted to blob storage;
/// only references (PhotoRef/FingerprintRef) are stored in the DB.
/// </summary>
public record NrbTextLookupResponseModel(
    // ── Data section ──
    string Nid,
    string FirstName,
    string? OtherNames,
    string Surname,
    string Gender,
    string MaritalStatus,
    string BirthDistrict,
    string ResidentialAddress,
    DateOnly DateOfBirth,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? TelephoneNumber,
    string CardStatus,
    // ── Document section ──
    string? PhotoBase64,
    string? FingerprintBase64,
    string? FingerPosition,
    string? Error,
    string? ErrorDescription,
    // ── Resolution ──
    bool IsFound
);

// ── Advanced (Tier 4) — Biometric + OTP, two-phase ───────────────────

public record NrbAdvancedRequestModel(
    string NationalId,
    string? BiometricBlob,
    string? Otp
);

public record NrbAdvancedResponseModel(
    bool IsSuccess,
    string? MaskedMobile,
    string? ConfirmationToken,
    string Phase
);

public static class NrbAdvancedPhase
{
    public const string OtpSent = "OTP_SENT";
    public const string VerificationComplete = "VERIFICATION_COMPLETE";
}

// ── Revalidation ─────────────────────────────────────────────────────

public record RevalidationResultDto(
    int TotalChecked,
    int Valid,
    int Expired,
    int Deceased,
    int SeeNrb,
    int Errors,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt
);
