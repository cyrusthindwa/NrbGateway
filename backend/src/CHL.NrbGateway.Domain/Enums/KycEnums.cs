namespace CHL.NrbGateway.Domain.Enums;

public enum OwnershipType
{
    LOCAL,
    FOREIGN
}

public enum AttachmentType
{
    CERT_OF_INCORPORATION,
    MEMARTS,
    UTILITY_BILL,
    SIGNATORY_ID_COPY,
    OTHER
}

public enum Title
{
    MR,
    MRS,
    MISS,
    MS,
    OTHER
}

public enum Gender
{
    MALE,
    FEMALE
}

public enum IdType
{
    NATIONAL_ID,
    TPIN,
    OTHER
}

public enum AddressType
{
    POSTAL,
    PHYSICAL,
    NRB_REGISTERED
}

public enum RecordStatus
{
    UNVERIFIED,
    PARTIALLY_VERIFIED,
    VERIFIED,
    NEEDS_CORRECTION
}

public enum VerificationSource
{
    SELF_DECLARED,
    NRB_BASIC,
    NRB_TEXT_LOOKUP,
    NRB_INTERMEDIATE,
    NRB_ADVANCED
}

public enum VerificationFieldStatus
{
    UNVERIFIED,
    CORRECT,
    INCORRECT,
    CORRECTED
}

public enum NrbTier
{
    BASIC,
    TEXT_LOOKUP,
    INTERMEDIATE,
    ADVANCED
}

public enum ServedFrom
{
    CACHE,
    NRB
}

public enum DocumentType
{
    FINGERPRINT,
    FACE,
    SIGNATURE
}

public enum DocumentSource
{
    TEXT_LOOKUP,
    ADVANCED
}

public enum FieldSource
{
    BASIC,
    TEXT_LOOKUP,
    ADVANCED
}

public enum ResponseMode
{
    MATCH_ONLY,
    OTP_SENT,
    DETAILED,
    FIELD_CHECK
}

public enum TriggerSource
{
    PROJECT_REQUEST,
    REVALIDATION
}
