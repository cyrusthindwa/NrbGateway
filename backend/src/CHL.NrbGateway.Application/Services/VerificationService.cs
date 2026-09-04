using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Application.Models;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Entities.Kyc;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CHL.NrbGateway.Application.Services;

public class VerificationService : IVerificationService
{
    private const string MatchStatus = "MATCH";
    private const string IdentityVerifiedStatus = "IDENTITY_VERIFIED";
    private const string NotFoundStatus = "NOT FOUND";

    private readonly IKycDbContext _kycDbContext;
    private readonly IConfigDbContext _configDbContext;
    private readonly INrbTierAdapter _nrbTierAdapter;
    private readonly IHmacService _hmacService;
    private readonly IEncryptionService _encryptionService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;
    private readonly INrbHealthMonitor _healthMonitor;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        IKycDbContext kycDbContext,
        IConfigDbContext configDbContext,
        INrbTierAdapter nrbTierAdapter,
        IHmacService hmacService,
        IEncryptionService encryptionService,
        IBlobStorageService blobStorageService,
        IConfiguration configuration,
        INrbHealthMonitor healthMonitor,
        ILogger<VerificationService> logger)
    {
        _kycDbContext = kycDbContext;
        _configDbContext = configDbContext;
        _nrbTierAdapter = nrbTierAdapter;
        _hmacService = hmacService;
        _encryptionService = encryptionService;
        _blobStorageService = blobStorageService;
        _configuration = configuration;
        _healthMonitor = healthMonitor;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════
    // INTERMEDIATE (Tier 3) — Biometric match, match-only response
    // ═══════════════════════════════════════════════════════════════════

    public async Task<IntermediateVerificationResultDto> VerifyIntermediateAsync(
        Guid projectId,
        string projectCode,
        IntermediateVerificationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var requestTimestamp = DateTimeOffset.UtcNow;
        var pinHash = _hmacService.ComputeHmacSha256(request.NationalId);

        EnsureTierEnabled(NrbTier.INTERMEDIATE);

        var subject = GetOrCreateSubject(pinHash, request.NationalId);

        // Cache-first (binary): a prior successful match serves regardless of age.
        var cached = _kycDbContext.NrbVerificationEvents
            .Where(e => e.PinSubmittedHash == pinHash
                     && e.Tier == NrbTier.INTERMEDIATE
                     && e.ResponseStatus == MatchStatus)
            .OrderByDescending(e => e.ResponseTimestamp)
            .FirstOrDefault();

        if (cached != null)
        {
            _logger.LogInformation("Serving Intermediate from CACHE for PIN {Hash}", pinHash);
            var cachedGw = PersistGatewayRequest(projectId, subject.SubjectId, ServedFrom.CACHE,
                cached.Id, cached.ResponseStatus, null, requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new IntermediateVerificationResultDto(cachedGw.Id, request.NationalId, true,
                cached.ResponseStatus, cached.ConfirmationToken, ServedFrom.CACHE, requestTimestamp);
        }

        NrbIntermediateResponseModel nrbResp;
        var responseTimestamp = DateTimeOffset.UtcNow;

        if (IsSimulationMode())
        {
            var hasMirror = _kycDbContext.Individuals.Any(i => i.SubjectId == subject.SubjectId);
            nrbResp = hasMirror
                ? new NrbIntermediateResponseModel(true, MatchStatus, $"SIM_CONF_{Guid.NewGuid():N}", null)
                : new NrbIntermediateResponseModel(false, "INVALID_PIN", null, null);
        }
        else
        {
            _logger.LogInformation("Calling NRB Intermediate live for PIN {Hash}", pinHash);
            nrbResp = await CallNrbAsync(
                () => _nrbTierAdapter.VerifyIntermediateAsync(
                    new NrbIntermediateRequestModel(request.NationalId, request.BiometricBlob, projectCode),
                    cancellationToken),
                cancellationToken);
            responseTimestamp = DateTimeOffset.UtcNow;
        }

        bool isMatch = nrbResp.IsMatch;
        string status = isMatch ? MatchStatus : (string.IsNullOrWhiteSpace(nrbResp.Status) ? "NO_MATCH" : nrbResp.Status);

        // A successful match with no flag raised → CLEAR; a data-bearing match
        // (match flag + confirmation token) creates the individuals mirror row.
        if (isMatch)
        {
            var individual = EnsureIndividual(subject.SubjectId, requestTimestamp);
            individual.MiddlewareStatus = "CLEAR";
            individual.LastMiddlewareCheckAt = responseTimestamp;
            individual.UpdatedAt = responseTimestamp;
        }

        var evt = PersistVerificationEvent(subject.SubjectId, pinHash, request.NationalId, NrbTier.INTERMEDIATE,
            projectCode, requestTimestamp, responseTimestamp, status, nrbResp.ConfirmationToken,
            ResponseMode.MATCH_ONLY, TriggerSource.PROJECT_REQUEST, null, nrbResp.RawResponsePayload);

        var gw = PersistGatewayRequest(projectId, subject.SubjectId, ServedFrom.NRB,
            evt.Id, status, TierCost(NrbTier.INTERMEDIATE), requestTimestamp);

        await _kycDbContext.SaveChangesAsync(cancellationToken);
        return new IntermediateVerificationResultDto(gw.Id, request.NationalId, isMatch,
            status, nrbResp.ConfirmationToken, ServedFrom.NRB, requestTimestamp);
    }

    // ═══════════════════════════════════════════════════════════════════
    // BASIC (Tier 1) — Always-live field reconciliation (a field CHECK)
    // ═══════════════════════════════════════════════════════════════════

    public async Task<BasicVerificationResultDto> VerifyBasicAsync(
        Guid projectId,
        string projectCode,
        BasicVerificationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var requestTimestamp = DateTimeOffset.UtcNow;
        var pinHash = _hmacService.ComputeHmacSha256(request.IdNumber);

        EnsureTierEnabled(NrbTier.BASIC);

        // A subject_id is assigned the moment ANY PIN is submitted.
        var subject = GetOrCreateSubject(pinHash, request.IdNumber);

        NrbBasicResponseModel nrbResp;
        var responseTimestamp = DateTimeOffset.UtcNow;

        if (IsSimulationMode())
        {
            nrbResp = SimulateBasicVerification(request, subject.SubjectId, pinHash);
        }
        else
        {
            _logger.LogInformation("Calling NRB Basic live for PIN {Hash} (always-live tier)", pinHash);
            nrbResp = await CallNrbAsync(
                () => _nrbTierAdapter.VerifyBasicAsync(
                    new NrbBasicRequestModel(
                        request.IdNumber, request.Surname, request.FirstName, request.OtherNames,
                        request.Nationality, request.Gender, request.DateOfBirthString,
                        request.DateOfIssueString, request.DateOfExpiryString, request.PlaceOfBirthDistrictName),
                    cancellationToken),
                cancellationToken);
            responseTimestamp = DateTimeOffset.UtcNow;
        }

        bool notFound = string.Equals(nrbResp.CardStatus, NrbBasicCardStatus.NotFound, StringComparison.OrdinalIgnoreCase);

        // Only a data-bearing response creates/updates the individuals mirror row.
        if (!notFound)
        {
            var individual = EnsureIndividual(subject.SubjectId, requestTimestamp);
            individual.CardStatus = nrbResp.CardStatus;
            individual.LastCardCheckAt = responseTimestamp;
            individual.UpdatedAt = responseTimestamp;
        }

        // Basic is a per-field check, not a data source → field check results.
        foreach (var (fieldName, result) in nrbResp.FieldResults)
        {
            PersistFieldCheckResult(subject.SubjectId, fieldName, result, NrbTier.BASIC, responseTimestamp);
        }

        var evt = PersistVerificationEvent(subject.SubjectId, pinHash, request.IdNumber, NrbTier.BASIC,
            projectCode, requestTimestamp, responseTimestamp, nrbResp.CardStatus, null,
            ResponseMode.FIELD_CHECK, TriggerSource.PROJECT_REQUEST, null, null);

        var gw = PersistGatewayRequest(projectId, subject.SubjectId, ServedFrom.NRB,
            evt.Id, nrbResp.CardStatus, TierCost(NrbTier.BASIC), requestTimestamp);

        await _kycDbContext.SaveChangesAsync(cancellationToken);
        return new BasicVerificationResultDto(gw.Id, request.IdNumber, nrbResp.CardStatus,
            nrbResp.FieldResults, ServedFrom.NRB, requestTimestamp);
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEXT LOOKUP (Tier 2) — Demographic retrieval, cache-first
    // ═══════════════════════════════════════════════════════════════════

    public async Task<TextLookupResultDto> TextLookupAsync(
        Guid projectId,
        string projectCode,
        TextLookupRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var requestTimestamp = DateTimeOffset.UtcNow;
        var pinHash = _hmacService.ComputeHmacSha256(request.IdNumber);

        EnsureTierEnabled(NrbTier.TEXT_LOOKUP);

        var subject = GetOrCreateSubject(pinHash, request.IdNumber);

        // Cache-first (binary): a prior successful Text Lookup serves regardless of age.
        var cached = _kycDbContext.NrbVerificationEvents
            .Where(e => e.PinSubmittedHash == pinHash
                     && e.Tier == NrbTier.TEXT_LOOKUP
                     && e.ResponseStatus == IdentityVerifiedStatus)
            .OrderByDescending(e => e.ResponseTimestamp)
            .FirstOrDefault();

        if (cached != null)
        {
            var cachedIndividual = _kycDbContext.Individuals.FirstOrDefault(i => i.SubjectId == cached.SubjectId);
            if (cachedIndividual != null)
            {
                _logger.LogInformation("Serving Text Lookup from CACHE for PIN {Hash}", pinHash);
                var (photoRef, fingerprintRef) = GetDocumentRefs(cachedIndividual.SubjectId);
                var cachedGw = PersistGatewayRequest(projectId, subject.SubjectId, ServedFrom.CACHE,
                    cached.Id, cached.ResponseStatus, null, requestTimestamp);
                await _kycDbContext.SaveChangesAsync(cancellationToken);
                return new TextLookupResultDto(
                    cachedGw.Id,
                    request.IdNumber,
                    cachedIndividual.Surname ?? "",
                    cachedIndividual.FirstName ?? "",
                    cachedIndividual.OtherNames,
                    cachedIndividual.DateOfBirth ?? DateOnly.MinValue,
                    cachedIndividual.Gender ?? "",
                    photoRef,
                    fingerprintRef,
                    ServedFrom.CACHE,
                    true,
                    requestTimestamp,
                    cachedIndividual.CardStatus ?? "VALID",
                    cachedIndividual.IdDateOfIssue,
                    cachedIndividual.IdDateOfExpiry,
                    cachedIndividual.Nationality ?? "MALAWIAN",
                    cachedIndividual.CivilStatus,
                    cachedIndividual.BirthDistrict,
                    cachedIndividual.ResidenceAddress,
                    cachedIndividual.NrbRegisteredPhone,
                    cachedIndividual.MiddlewareStatus ?? "CLEAR"
                );
            }
        }

        if (IsSimulationMode())
        {
            var simIndividual = _kycDbContext.Individuals.FirstOrDefault(i => i.SubjectId == subject.SubjectId);
            if (simIndividual == null)
            {
                simIndividual = new Individual
                {
                    SubjectId = subject.SubjectId,
                    Surname = "BANDA",
                    FirstName = "CHIKONDI",
                    OtherNames = "JOHN",
                    DateOfBirth = new DateOnly(1990, 5, 15),
                    Gender = "MALE",
                    Nationality = "MALAWIAN",
                    CivilStatus = "MARRIED",
                    BirthDistrict = "LILONGWE",
                    ResidenceAddress = "Plot 12, Area 10, Lilongwe",
                    NrbRegisteredPhone = "+265999000111",
                    CardStatus = "VALID",
                    MiddlewareStatus = "CLEAR",
                    IdDateOfIssue = new DateOnly(2020, 1, 10),
                    IdDateOfExpiry = new DateOnly(2030, 1, 10),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _kycDbContext.Add(simIndividual);
                await _kycDbContext.SaveChangesAsync(cancellationToken);
            }

            var (sPhoto, sFinger) = GetDocumentRefs(simIndividual.SubjectId);
            var simGw = PersistGatewayRequest(projectId, subject.SubjectId, ServedFrom.CACHE, null,
                IdentityVerifiedStatus, null, requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new TextLookupResultDto(
                simGw.Id,
                request.IdNumber,
                simIndividual.Surname ?? "",
                simIndividual.FirstName ?? "",
                simIndividual.OtherNames,
                simIndividual.DateOfBirth ?? DateOnly.MinValue,
                simIndividual.Gender ?? "",
                sPhoto,
                sFinger,
                ServedFrom.CACHE,
                true,
                requestTimestamp,
                simIndividual.CardStatus ?? "VALID",
                simIndividual.IdDateOfIssue,
                simIndividual.IdDateOfExpiry,
                simIndividual.Nationality ?? "MALAWIAN",
                simIndividual.CivilStatus ?? "MARRIED",
                simIndividual.BirthDistrict ?? "LILONGWE",
                simIndividual.ResidenceAddress ?? "Plot 12, Area 10, Lilongwe",
                simIndividual.NrbRegisteredPhone ?? "+265999000111",
                simIndividual.MiddlewareStatus ?? "CLEAR"
            );
        }

        _logger.LogInformation("Calling NRB Text Lookup live for PIN {Hash}", pinHash);
        var nrbResp = await CallNrbAsync(
            () => _nrbTierAdapter.TextLookupAsync(
                new NrbTextLookupRequestModel(request.IdNumber), cancellationToken),
            cancellationToken);
        var responseTimestamp = DateTimeOffset.UtcNow;

        if (!nrbResp.IsFound)
        {
            _logger.LogWarning("NRB Text Lookup: PIN {Hash} not found in registry.", pinHash);
            var nfGw = PersistGatewayRequest(projectId, subject.SubjectId, ServedFrom.NRB, null,
                NotFoundStatus, TierCost(NrbTier.TEXT_LOOKUP), requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new TextLookupResultDto(
                nfGw.Id, request.IdNumber,
                "", "", null, DateOnly.MinValue, "", null, null, ServedFrom.NRB, false, requestTimestamp,
                "NOT FOUND", null, null, null, null, null, null, null, null);
        }

        var individual = EnsureIndividual(subject.SubjectId, requestTimestamp);
        ApplyTextLookupData(individual, nrbResp, responseTimestamp);

        await PersistDocumentAsync(subject.SubjectId, DocumentType.FACE, DocumentSource.TEXT_LOOKUP,
            null, null, nrbResp.PhotoBase64, responseTimestamp, cancellationToken);
        await PersistDocumentAsync(subject.SubjectId, DocumentType.FINGERPRINT, DocumentSource.TEXT_LOOKUP,
            null, nrbResp.FingerPosition, nrbResp.FingerprintBase64, responseTimestamp, cancellationToken);

        var evt = PersistVerificationEvent(subject.SubjectId, pinHash, request.IdNumber, NrbTier.TEXT_LOOKUP,
            projectCode, requestTimestamp, responseTimestamp, IdentityVerifiedStatus, null,
            ResponseMode.DETAILED, TriggerSource.PROJECT_REQUEST, null, null);

        var gw = PersistGatewayRequest(projectId, subject.SubjectId, ServedFrom.NRB,
            evt.Id, IdentityVerifiedStatus, TierCost(NrbTier.TEXT_LOOKUP), requestTimestamp);

        await _kycDbContext.SaveChangesAsync(cancellationToken);

        var (faceRef, fingerRef) = GetDocumentRefs(subject.SubjectId);
        return new TextLookupResultDto(
            gw.Id,
            request.IdNumber,
            nrbResp.Surname,
            nrbResp.FirstName,
            nrbResp.OtherNames,
            nrbResp.DateOfBirth,
            nrbResp.Gender,
            faceRef,
            fingerRef,
            ServedFrom.NRB,
            true,
            requestTimestamp,
            nrbResp.CardStatus ?? "VALID",
            nrbResp.IssueDate,
            nrbResp.ExpiryDate,
            "MALAWIAN",
            nrbResp.MaritalStatus,
            nrbResp.BirthDistrict,
            nrbResp.ResidentialAddress,
            nrbResp.TelephoneNumber,
            individual.MiddlewareStatus ?? "CLEAR"
        );
    }

    // ═══════════════════════════════════════════════════════════════════
    // ADVANCED (Tier 4) — Biometric + OTP, branch on actual response mode
    // ═══════════════════════════════════════════════════════════════════

    public async Task<AdvancedVerificationResultDto> VerifyAdvancedAsync(
        Guid projectId,
        string projectCode,
        AdvancedVerificationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var requestTimestamp = DateTimeOffset.UtcNow;
        var pinHash = _hmacService.ComputeHmacSha256(request.NationalId);

        EnsureTierEnabled(NrbTier.ADVANCED);

        var subject = GetOrCreateSubject(pinHash, request.NationalId);

        _logger.LogInformation("Calling NRB Advanced live for PIN {Hash}", pinHash);

        NrbAdvancedResponseModel nrbResp;
        var responseTimestamp = DateTimeOffset.UtcNow;

        if (IsSimulationMode())
        {
            nrbResp = SimulateAdvanced(request);
        }
        else
        {
            nrbResp = await CallNrbAsync(
                () => _nrbTierAdapter.VerifyAdvancedAsync(
                    new NrbAdvancedRequestModel(request.NationalId, request.BiometricBlob, request.Otp),
                    cancellationToken),
                cancellationToken);
            responseTimestamp = DateTimeOffset.UtcNow;
        }

        // Phase 1 (OTP_SENT): log the request, no verification result yet.
        if (nrbResp.ResponseMode == ResponseMode.OTP_SENT)
        {
            var p1Gw = PersistGatewayRequest(projectId, subject.SubjectId, ServedFrom.NRB, null,
                "OTP_SENT", TierCost(NrbTier.ADVANCED), requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new AdvancedVerificationResultDto(p1Gw.Id, request.NationalId,
                nrbResp.IsSuccess, nrbResp.MaskedMobile, null, NrbAdvancedPhase.OtpSent, requestTimestamp);
        }

        if (!nrbResp.IsSuccess)
        {
            var failGw = PersistGatewayRequest(projectId, subject.SubjectId, ServedFrom.NRB, null,
                "VERIFICATION_FAILED", TierCost(NrbTier.ADVANCED), requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new AdvancedVerificationResultDto(failGw.Id, request.NationalId,
                false, nrbResp.MaskedMobile, null, nrbResp.Phase, requestTimestamp);
        }

        // Detailed / direct success: populate the mirror with returned data.
        var individual = EnsureIndividual(subject.SubjectId, requestTimestamp);

        if (nrbResp.Person != null)
        {
            ApplyAdvancedPersonData(individual, nrbResp.Person, responseTimestamp);
        }

        if (nrbResp.Blobs != null)
        {
            foreach (var blob in nrbResp.Blobs)
            {
                await PersistAdvancedBlobAsync(subject.SubjectId, blob, responseTimestamp, cancellationToken);
            }
        }

        var evt = PersistVerificationEvent(subject.SubjectId, pinHash, request.NationalId, NrbTier.ADVANCED,
            projectCode, requestTimestamp, responseTimestamp, IdentityVerifiedStatus, nrbResp.ConfirmationToken,
            ResponseMode.DETAILED, TriggerSource.PROJECT_REQUEST, null, null);

        var gw = PersistGatewayRequest(projectId, subject.SubjectId, ServedFrom.NRB,
            evt.Id, IdentityVerifiedStatus, TierCost(NrbTier.ADVANCED), requestTimestamp);

        await _kycDbContext.SaveChangesAsync(cancellationToken);
        return new AdvancedVerificationResultDto(gw.Id, request.NationalId,
            true, nrbResp.MaskedMobile, nrbResp.ConfirmationToken,
            NrbAdvancedPhase.VerificationComplete, requestTimestamp);
    }

    // ═══════════════════════════════════════════════════════════════════
    // REVALIDATION — Admin/scheduled batch re-check of local mirror vs NRB
    // ═══════════════════════════════════════════════════════════════════

    public async Task<RevalidationResultDto> RevalidateAllAsync(
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        int total = 0, valid = 0, expired = 0, deceased = 0, seeNrb = 0, errors = 0;

        var batch = new RevalidationBatch
        {
            Id = Guid.NewGuid(),
            TriggerType = RevalidationTriggerType.MANUAL,
            InitiatedBy = adminId,
            StartedAt = startedAt
        };
        _configDbContext.Add(batch);
        await _configDbContext.SaveChangesAsync(cancellationToken);

        var subjects = _kycDbContext.IdentityLookups.ToList();
        total = subjects.Count;

        _logger.LogInformation("REVALIDATION: Checking {Count} PINs against NRB Basic tier.", total);

        foreach (var subject in subjects)
        {
            try
            {
                var nationalId = _encryptionService.Decrypt(subject.NationalIdEncrypted);
                if (string.IsNullOrEmpty(nationalId)) { errors++; continue; }

                var individual = _kycDbContext.Individuals.FirstOrDefault(i => i.SubjectId == subject.SubjectId);
                if (individual == null) continue;

                var nrbReq = new NrbBasicRequestModel(
                    nationalId, individual.Surname ?? "", individual.FirstName ?? "", individual.OtherNames,
                    "", individual.Gender ?? "", individual.DateOfBirth?.ToString("yyyy-MM-dd") ?? "",
                    individual.IdDateOfIssue?.ToString("yyyy-MM-dd"),
                    individual.IdDateOfExpiry?.ToString("yyyy-MM-dd"),
                    individual.BirthDistrict);

                var nrbResp = await CallNrbAsync(() => _nrbTierAdapter.VerifyBasicAsync(nrbReq, cancellationToken), cancellationToken);

                bool statusChanged = !string.Equals(individual.CardStatus, nrbResp.CardStatus, StringComparison.OrdinalIgnoreCase);
                individual.CardStatus = nrbResp.CardStatus;
                individual.LastCardCheckAt = DateTimeOffset.UtcNow;
                individual.UpdatedAt = DateTimeOffset.UtcNow;

                bool anyIncorrect = false;
                foreach (var (fieldName, result) in nrbResp.FieldResults)
                {
                    PersistFieldCheckResult(subject.SubjectId, fieldName, result, NrbTier.BASIC, DateTimeOffset.UtcNow);
                    if (string.Equals(result, "INCORRECT", StringComparison.OrdinalIgnoreCase)) anyIncorrect = true;
                }

                PersistVerificationEvent(subject.SubjectId, subject.NationalIdHash, nationalId, NrbTier.BASIC,
                    "REVALIDATION", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, nrbResp.CardStatus, null,
                    ResponseMode.FIELD_CHECK, TriggerSource.REVALIDATION, batch.Id, null);

                // Refresh actual values only via a data-bearing tier (Text Lookup
                // or Advanced) — never Intermediate, which returns no biographic data.
                if (anyIncorrect || statusChanged)
                {
                    await RefreshFromDataBearingTierAsync(subject, individual, batch.Id, cancellationToken);
                }

                switch (nrbResp.CardStatus)
                {
                    case NrbBasicCardStatus.Valid: valid++; break;
                    case NrbBasicCardStatus.Expired: expired++; break;
                    case NrbBasicCardStatus.PersonDeceased: deceased++; break;
                    case NrbBasicCardStatus.SeeNrb: seeNrb++; break;
                    default: errors++; break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "REVALIDATION: Failed for PIN hash {Hash}", subject.NationalIdHash);
                errors++;
            }
        }

        await _kycDbContext.SaveChangesAsync(cancellationToken);

        batch.CompletedAt = DateTimeOffset.UtcNow;
        batch.TotalCount = total;
        batch.ValidCount = valid;
        batch.ExpiredCount = expired;
        batch.DeceasedCount = deceased;
        batch.SeeNrbCount = seeNrb;
        batch.ErrorCount = errors;
        _configDbContext.Update(batch);

        _configDbContext.Add(new ConfigAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            SettingArea = SettingArea.NRB_ENVIRONMENT,
            SettingKey = "revalidation.batch",
            OldValue = null,
            NewValue = $"Checked {total}: {valid} valid, {expired} expired, {deceased} deceased, {seeNrb} see NRB, {errors} errors",
            ChangedAt = DateTimeOffset.UtcNow
        });
        await _configDbContext.SaveChangesAsync(cancellationToken);

        var completedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation("REVALIDATION complete. Total={Total}, Valid={Valid}, Expired={Expired}, Deceased={Deceased}, SeeNrb={SeeNrb}, Errors={Errors}",
            total, valid, expired, deceased, seeNrb, errors);

        return new RevalidationResultDto(total, valid, expired, deceased, seeNrb, errors, startedAt, completedAt);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Shared helpers
    // ═══════════════════════════════════════════════════════════════════

    private bool IsSimulationMode() =>
        bool.TryParse(_configuration["Nrb:SimulationMode"], out var sim) && sim;

    private async Task<T> CallNrbAsync<T>(Func<Task<T>> call, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        try
        {
            var result = await call();
            await _healthMonitor.RecordAsync(true, (int)(DateTimeOffset.UtcNow - started).TotalMilliseconds, null, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            await _healthMonitor.RecordAsync(false, null, ex.Message, cancellationToken);
            throw;
        }
    }

    private void EnsureTierEnabled(NrbTier tier)
    {
        var setting = _configDbContext.VerificationTierSettings.FirstOrDefault(t => t.Tier == tier);
        if (setting != null && !setting.Enabled)
            throw new InvalidOperationException($"The {tier} NRB verification tier is currently disabled.");
    }

    private decimal? TierCost(NrbTier tier) =>
        _configDbContext.VerificationTierSettings.FirstOrDefault(t => t.Tier == tier)?.CostPerRequest;

    private IdentityLookup GetOrCreateSubject(string pinHash, string rawPin)
    {
        var existing = _kycDbContext.IdentityLookups.FirstOrDefault(i => i.NationalIdHash == pinHash);
        if (existing != null) return existing;

        var subject = new IdentityLookup
        {
            SubjectId = Guid.NewGuid(),
            NationalIdHash = pinHash,
            NationalIdEncrypted = _encryptionService.Encrypt(rawPin),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _kycDbContext.Add(subject);
        return subject;
    }

    private Individual EnsureIndividual(Guid subjectId, DateTimeOffset createdAt)
    {
        var existing = _kycDbContext.Individuals.FirstOrDefault(i => i.SubjectId == subjectId);
        if (existing != null) return existing;

        var individual = new Individual
        {
            SubjectId = subjectId,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
        _kycDbContext.Add(individual);
        return individual;
    }

    private NrbVerificationEvent PersistVerificationEvent(
        Guid? subjectId, string pinHash, string rawPin, NrbTier tier, string projectCode,
        DateTimeOffset requestTimestamp, DateTimeOffset responseTimestamp, string status,
        string? confirmationToken, ResponseMode responseMode, TriggerSource triggerSource,
        Guid? revalidationBatchId, string? rawPayload)
    {
        var evt = new NrbVerificationEvent
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            PinSubmittedHash = pinHash,
            PinSubmittedEncrypted = _encryptionService.Encrypt(rawPin),
            Tier = tier,
            RequestingProjectCode = projectCode,
            ResponseMode = responseMode,
            TriggerSource = triggerSource,
            RequestTimestamp = requestTimestamp,
            ResponseTimestamp = responseTimestamp,
            ResponseStatus = status,
            ConfirmationToken = confirmationToken,
            RawResponseRef = rawPayload,
            RevalidationBatchId = revalidationBatchId
        };
        _kycDbContext.Add(evt);
        return evt;
    }

    private GatewayRequest PersistGatewayRequest(
        Guid projectId, Guid? subjectId, ServedFrom servedFrom, Guid? eventId,
        string status, decimal? cost, DateTimeOffset timestamp)
    {
        var gw = new GatewayRequest
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SubjectId = subjectId,
            ServedFrom = servedFrom,
            NrbVerificationEventId = eventId,
            ResponseStatus = status,
            CostIncurred = cost,
            RequestTimestamp = timestamp
        };
        _kycDbContext.Add(gw);
        return gw;
    }

    private void PersistFieldCheckResult(Guid? subjectId, string fieldName, string result, NrbTier tier, DateTimeOffset checkedAt)
    {
        _kycDbContext.Add(new NrbFieldCheckResult
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            FieldName = fieldName,
            Result = result,
            Tier = tier,
            CheckedAt = checkedAt
        });
    }

    private void PersistSourceValue(Guid subjectId, string fieldName, string value, FieldSource source, DateTimeOffset observedAt)
    {
        var current = _kycDbContext.IndividualSourceValues
            .Where(v => v.SubjectId == subjectId && v.FieldName == fieldName && v.IsCurrent)
            .ToList();
        foreach (var prior in current)
        {
            prior.IsCurrent = false;
            _kycDbContext.Update(prior);
        }

        _kycDbContext.Add(new IndividualSourceValue
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            FieldName = fieldName,
            Value = value,
            Source = source,
            ObservedAt = observedAt,
            IsCurrent = true
        });
    }

    private async Task PersistDocumentAsync(
        Guid subjectId, DocumentType documentType, DocumentSource source, string? blobFormat,
        string? fingerPosition, string? base64Data, DateTimeOffset capturedAt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(base64Data)) return;

        byte[] bytes;
        try { bytes = Convert.FromBase64String(base64Data); }
        catch { return; }

        var blobRef = await _blobStorageService.StoreAsync(
            subjectId.ToString("N"), documentType.ToString().ToLowerInvariant(), blobFormat, bytes, cancellationToken);

        if (blobRef == null)
        {
            _logger.LogWarning("Blob storage failed; skipping {DocumentType} document row for subject {SubjectId}",
                documentType, subjectId);
            return;
        }

        _kycDbContext.Add(new IndividualDocument
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            DocumentType = documentType,
            Source = source,
            BlobFormat = blobFormat,
            FingerPosition = fingerPosition,
            BlobRef = blobRef,
            CapturedAt = capturedAt
        });
    }

    private async Task PersistAdvancedBlobAsync(
        Guid subjectId, NrbAdvancedBlob blob, DateTimeOffset capturedAt, CancellationToken cancellationToken)
    {
        var documentType = MapDocumentType(blob.Description);
        if (documentType == null) return;

        await PersistDocumentAsync(subjectId, documentType.Value, DocumentSource.ADVANCED, blob.BlobType,
            documentType == DocumentType.FINGERPRINT ? blob.BlobIndex : null, blob.Data, capturedAt, cancellationToken);
    }

    private static DocumentType? MapDocumentType(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        if (description.Contains("finger", StringComparison.OrdinalIgnoreCase)) return DocumentType.FINGERPRINT;
        if (description.Contains("sign", StringComparison.OrdinalIgnoreCase)) return DocumentType.SIGNATURE;
        if (description.Contains("face", StringComparison.OrdinalIgnoreCase)
            || description.Contains("photo", StringComparison.OrdinalIgnoreCase)
            || description.Contains("image", StringComparison.OrdinalIgnoreCase)) return DocumentType.FACE;
        return null;
    }

    private void ApplyTextLookupData(Individual individual, NrbTextLookupResponseModel resp, DateTimeOffset observedAt)
    {
        individual.Surname = resp.Surname;
        individual.FirstName = resp.FirstName;
        individual.OtherNames = resp.OtherNames;
        individual.DateOfBirth = resp.DateOfBirth == DateOnly.MinValue ? null : resp.DateOfBirth;
        individual.Gender = resp.Gender;
        individual.CivilStatus = resp.MaritalStatus;
        individual.BirthDistrict = resp.BirthDistrict;
        individual.ResidenceAddress = resp.ResidentialAddress;
        individual.NrbRegisteredPhone = resp.TelephoneNumber;
        individual.IdDateOfIssue = resp.IssueDate;
        individual.IdDateOfExpiry = resp.ExpiryDate;
        individual.CardStatus = resp.CardStatus;
        individual.LastCardCheckAt = observedAt;
        individual.UpdatedAt = observedAt;

        PersistSourceValue(individual.SubjectId, "surname", resp.Surname, FieldSource.TEXT_LOOKUP, observedAt);
        PersistSourceValue(individual.SubjectId, "first_name", resp.FirstName, FieldSource.TEXT_LOOKUP, observedAt);
        if (!string.IsNullOrWhiteSpace(resp.OtherNames))
            PersistSourceValue(individual.SubjectId, "other_names", resp.OtherNames, FieldSource.TEXT_LOOKUP, observedAt);
        if (resp.DateOfBirth != DateOnly.MinValue)
            PersistSourceValue(individual.SubjectId, "date_of_birth", resp.DateOfBirth.ToString("yyyy-MM-dd"), FieldSource.TEXT_LOOKUP, observedAt);
        if (!string.IsNullOrWhiteSpace(resp.Gender))
            PersistSourceValue(individual.SubjectId, "gender", resp.Gender, FieldSource.TEXT_LOOKUP, observedAt);
        if (!string.IsNullOrWhiteSpace(resp.MaritalStatus))
            PersistSourceValue(individual.SubjectId, "civil_status", resp.MaritalStatus, FieldSource.TEXT_LOOKUP, observedAt);
        if (!string.IsNullOrWhiteSpace(resp.BirthDistrict))
            PersistSourceValue(individual.SubjectId, "birth_district", resp.BirthDistrict, FieldSource.TEXT_LOOKUP, observedAt);
        if (!string.IsNullOrWhiteSpace(resp.ResidentialAddress))
            PersistSourceValue(individual.SubjectId, "residence_address", resp.ResidentialAddress, FieldSource.TEXT_LOOKUP, observedAt);
        if (!string.IsNullOrWhiteSpace(resp.TelephoneNumber))
            PersistSourceValue(individual.SubjectId, "nrb_registered_phone", resp.TelephoneNumber, FieldSource.TEXT_LOOKUP, observedAt);
        if (resp.IssueDate.HasValue)
            PersistSourceValue(individual.SubjectId, "id_date_of_issue", resp.IssueDate.Value.ToString("yyyy-MM-dd"), FieldSource.TEXT_LOOKUP, observedAt);
        if (resp.ExpiryDate.HasValue)
            PersistSourceValue(individual.SubjectId, "id_date_of_expiry", resp.ExpiryDate.Value.ToString("yyyy-MM-dd"), FieldSource.TEXT_LOOKUP, observedAt);
        if (!string.IsNullOrWhiteSpace(resp.CardStatus))
            PersistSourceValue(individual.SubjectId, "card_status", resp.CardStatus, FieldSource.TEXT_LOOKUP, observedAt);
    }

    private void ApplyAdvancedPersonData(Individual individual, NrbAdvancedPersonData person, DateTimeOffset observedAt)
    {
        if (!string.IsNullOrWhiteSpace(person.Surname)) { individual.Surname = person.Surname; PersistSourceValue(individual.SubjectId, "surname", person.Surname, FieldSource.ADVANCED, observedAt); }
        if (!string.IsNullOrWhiteSpace(person.FirstName)) { individual.FirstName = person.FirstName; PersistSourceValue(individual.SubjectId, "first_name", person.FirstName, FieldSource.ADVANCED, observedAt); }
        if (!string.IsNullOrWhiteSpace(person.OtherNames)) { individual.OtherNames = person.OtherNames; PersistSourceValue(individual.SubjectId, "other_names", person.OtherNames, FieldSource.ADVANCED, observedAt); }
        if (!string.IsNullOrWhiteSpace(person.Gender)) { individual.Gender = person.Gender; PersistSourceValue(individual.SubjectId, "gender", person.Gender, FieldSource.ADVANCED, observedAt); }
        if (!string.IsNullOrWhiteSpace(person.CivilStatus)) { individual.CivilStatus = person.CivilStatus; PersistSourceValue(individual.SubjectId, "civil_status", person.CivilStatus, FieldSource.ADVANCED, observedAt); }
        if (!string.IsNullOrWhiteSpace(person.BirthDistrict)) { individual.BirthDistrict = person.BirthDistrict; PersistSourceValue(individual.SubjectId, "birth_district", person.BirthDistrict, FieldSource.ADVANCED, observedAt); }
        if (!string.IsNullOrWhiteSpace(person.PlaceOfPermanentResidence)) { individual.ResidenceAddress = person.PlaceOfPermanentResidence; PersistSourceValue(individual.SubjectId, "residence_address", person.PlaceOfPermanentResidence, FieldSource.ADVANCED, observedAt); }
        if (person.DateOfBirth.HasValue) { individual.DateOfBirth = person.DateOfBirth; PersistSourceValue(individual.SubjectId, "date_of_birth", person.DateOfBirth.Value.ToString("yyyy-MM-dd"), FieldSource.ADVANCED, observedAt); }
        if (person.IssueDate.HasValue) { individual.IdDateOfIssue = person.IssueDate; PersistSourceValue(individual.SubjectId, "id_date_of_issue", person.IssueDate.Value.ToString("yyyy-MM-dd"), FieldSource.ADVANCED, observedAt); }
        if (person.ExpiryDate.HasValue) { individual.IdDateOfExpiry = person.ExpiryDate; PersistSourceValue(individual.SubjectId, "id_date_of_expiry", person.ExpiryDate.Value.ToString("yyyy-MM-dd"), FieldSource.ADVANCED, observedAt); }
        if (!string.IsNullOrWhiteSpace(person.CardStatus)) { individual.CardStatus = person.CardStatus; individual.LastCardCheckAt = observedAt; }
        if (!string.IsNullOrWhiteSpace(person.MiddlewareStatus)) { individual.MiddlewareStatus = person.MiddlewareStatus; individual.LastMiddlewareCheckAt = observedAt; }
        individual.UpdatedAt = observedAt;
    }

    private (string? face, string? fingerprint) GetDocumentRefs(Guid subjectId)
    {
        var docs = _kycDbContext.IndividualDocuments.Where(d => d.SubjectId == subjectId).ToList();
        var face = docs.LastOrDefault(d => d.DocumentType == DocumentType.FACE)?.BlobRef;
        var fingerprint = docs.LastOrDefault(d => d.DocumentType == DocumentType.FINGERPRINT)?.BlobRef;
        return (face, fingerprint);
    }

    private async Task RefreshFromDataBearingTierAsync(
        IdentityLookup subject, Individual individual, Guid batchId, CancellationToken cancellationToken)
    {
        var nationalId = _encryptionService.Decrypt(subject.NationalIdEncrypted);
        if (string.IsNullOrEmpty(nationalId)) return;

        // Text Lookup is the biographic data tier. Advanced requires an
        // interactive biometric/OTP exchange and is not suitable for an
        // unattended refresh, so Text Lookup is used when enabled.
        var textEnabled = _configDbContext.VerificationTierSettings
            .Any(t => t.Tier == NrbTier.TEXT_LOOKUP && t.Enabled);
        if (!textEnabled)
        {
            _logger.LogWarning("REVALIDATION: no data-bearing tier enabled to refresh subject {SubjectId}", subject.SubjectId);
            return;
        }

        var resp = await CallNrbAsync(() => _nrbTierAdapter.TextLookupAsync(new NrbTextLookupRequestModel(nationalId), cancellationToken), cancellationToken);
        if (!resp.IsFound) return;

        ApplyTextLookupData(individual, resp, DateTimeOffset.UtcNow);

        await PersistDocumentAsync(subject.SubjectId, DocumentType.FACE, DocumentSource.TEXT_LOOKUP,
            null, null, resp.PhotoBase64, DateTimeOffset.UtcNow, cancellationToken);
        await PersistDocumentAsync(subject.SubjectId, DocumentType.FINGERPRINT, DocumentSource.TEXT_LOOKUP,
            null, resp.FingerPosition, resp.FingerprintBase64, DateTimeOffset.UtcNow, cancellationToken);

        PersistVerificationEvent(subject.SubjectId, subject.NationalIdHash, nationalId, NrbTier.TEXT_LOOKUP,
            "REVALIDATION", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, IdentityVerifiedStatus, null,
            ResponseMode.DETAILED, TriggerSource.REVALIDATION, batchId, null);
    }

    /// <summary>
    /// Simulation mode: compares submitted Basic verification fields against
    /// the locally-stored individual record.
    /// </summary>
    private NrbBasicResponseModel SimulateBasicVerification(
        BasicVerificationRequestDto request, Guid subjectId, string pinHash)
    {
        var individual = _kycDbContext.Individuals.FirstOrDefault(i => i.SubjectId == subjectId);
        if (individual == null)
        {
            _logger.LogInformation("SIMULATION: No local record for PIN {Hash} — NOT FOUND.", pinHash);
            return new NrbBasicResponseModel(NrbBasicCardStatus.NotFound,
                new Dictionary<string, string> { ["IdNumber"] = "INCORRECT" });
        }

        var f = new Dictionary<string, string>
        {
            ["Surname"] = string.Equals(request.Surname, individual.Surname, StringComparison.OrdinalIgnoreCase) ? "CORRECT" : "INCORRECT",
            ["FirstName"] = string.Equals(request.FirstName, individual.FirstName, StringComparison.OrdinalIgnoreCase) ? "CORRECT" : "INCORRECT",
            ["OtherNames"] = string.Equals(request.OtherNames ?? "", individual.OtherNames ?? "", StringComparison.OrdinalIgnoreCase) ? "CORRECT" : "INCORRECT",
            ["Gender"] = string.Equals(request.Gender, individual.Gender, StringComparison.OrdinalIgnoreCase) ? "CORRECT" : "INCORRECT",
            ["DateOfBirth"] = request.DateOfBirthString == individual.DateOfBirth?.ToString("yyyy-MM-dd") ? "CORRECT" : "INCORRECT",
            ["DateOfIssue"] = "CORRECT",
            ["DateOfExpiry"] = "CORRECT",
            ["PlaceOfBirthDistrict"] = string.Equals(request.PlaceOfBirthDistrictName ?? "", individual.BirthDistrict ?? "", StringComparison.OrdinalIgnoreCase) ? "CORRECT" : "INCORRECT"
        };

        string cardStatus = individual.CardStatus ?? NrbBasicCardStatus.Valid;

        _logger.LogInformation("SIMULATION: {FieldCount} fields compared. Card: {Status}", f.Count, cardStatus);
        return new NrbBasicResponseModel(cardStatus, f);
    }

    private NrbAdvancedResponseModel SimulateAdvanced(AdvancedVerificationRequestDto request)
    {
        bool phase1 = string.IsNullOrEmpty(request.Otp);
        if (phase1)
        {
            return new NrbAdvancedResponseModel(true, "088****234", null,
                NrbAdvancedPhase.OtpSent, ResponseMode.OTP_SENT, null, null);
        }

        return new NrbAdvancedResponseModel(true, "088****234", $"SIM_ADV_{Guid.NewGuid():N}",
            NrbAdvancedPhase.VerificationComplete, ResponseMode.DETAILED,
            new NrbAdvancedPersonData("Thindwa", "Cyrus", null, "MALE", null, null, null,
                new DateOnly(1990, 1, 1), null, null, "VALID", "CLEAR"),
            null);
    }
}
