using System.ComponentModel.DataAnnotations;
using CHL.NrbGateway.Domain.Enums;

namespace CHL.NrbGateway.Application.DTOs;

// ── Intermediate (Tier 3) ────────────────────────────────────────────

public record IntermediateVerificationRequestDto(
    [Required] string NationalId,
    [Required] string BiometricBlob
);

public record IntermediateVerificationResultDto(
    Guid VerificationId,
    string NationalId,
    bool IsMatch,
    string Status,
    string? ConfirmationToken,
    ServedFrom ServedFrom,
    DateTimeOffset Timestamp
);

// ── Basic (Tier 1) — Field reconciliation ────────────────────────────

public record BasicVerificationRequestDto(
    [Required] string IdNumber,
    [Required] string Surname,
    [Required] string FirstName,
    string? OtherNames,
    [Required] string Nationality,
    [Required] string Gender,
    [Required] string DateOfBirthString,
    string? DateOfIssueString,
    string? DateOfExpiryString,
    string? PlaceOfBirthDistrictName
);

public record BasicVerificationResultDto(
    Guid VerificationId,
    string NationalId,
    string CardStatus,
    Dictionary<string, string> FieldResults,   // fieldName → "CORRECT" | "INCORRECT"
    ServedFrom ServedFrom,
    DateTimeOffset Timestamp
);

// ── Text Lookup (Tier 2) — Demographic retrieval ─────────────────────

public record TextLookupRequestDto(
    [Required] string IdNumber
);

public record TextLookupResultDto(
    Guid VerificationId,
    string IdNumber,
    string Surname,
    string FirstName,
    string? OtherNames,
    DateOnly DateOfBirth,
    string Gender,
    string? PhotoRef,            // Pointer to blob storage, not inline base64
    string? FingerprintRef,      // Pointer to blob storage, not inline base64
    ServedFrom ServedFrom,
    bool Found,                  // false → ID not found in NRB registry
    DateTimeOffset Timestamp,
    string? CardStatus = "VALID",
    DateOnly? IssueDate = null,
    DateOnly? ExpiryDate = null
);

// ── Advanced (Tier 4) — Biometric + OTP ──────────────────────────────

public record AdvancedVerificationRequestDto(
    [Required] string NationalId,
    string? BiometricBlob,       // Phase 1: non-null; Phase 2: null/empty
    string? Otp                  // Phase 1: null/empty; Phase 2: OTP from SMS
);

public record AdvancedVerificationResultDto(
    Guid VerificationId,
    string NationalId,
    bool IsSuccess,
    string? MaskedMobile,
    string? ConfirmationToken,
    string Phase,                // "OTP_SENT" | "VERIFICATION_COMPLETE"
    DateTimeOffset Timestamp
);
