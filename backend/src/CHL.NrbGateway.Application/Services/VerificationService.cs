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
    private readonly IKycDbContext _kycDbContext;
    private readonly IConfigDbContext _configDbContext;
    private readonly INrbTierAdapter _nrbTierAdapter;
    private readonly IHmacService _hmacService;
    private readonly IEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        IKycDbContext kycDbContext,
        IConfigDbContext configDbContext,
        INrbTierAdapter nrbTierAdapter,
        IHmacService hmacService,
        IEncryptionService encryptionService,
        IConfiguration configuration,
        ILogger<VerificationService> logger)
    {
        _kycDbContext = kycDbContext;
        _configDbContext = configDbContext;
        _nrbTierAdapter = nrbTierAdapter;
        _hmacService = hmacService;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════
    // INTERMEDIATE (Tier 3) — Biometric match, cache-first by config
    // ═══════════════════════════════════════════════════════════════════

    public async Task<IntermediateVerificationResultDto> VerifyIntermediateAsync(
        Guid subsidiaryId,
        string subsidiaryShortCode,
        IntermediateVerificationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var requestTimestamp = DateTimeOffset.UtcNow;
        var pinHash = _hmacService.ComputeHmacSha256(request.NationalId);

        // 1. Verify tier toggle
        var tierSetting = _configDbContext.VerificationTierSettings
            .FirstOrDefault(t => t.Tier == NrbTier.INTERMEDIATE);
        if (tierSetting != null && !tierSetting.Enabled)
            throw new InvalidOperationException("The INTERMEDIATE NRB verification tier is currently disabled.");

        // 2. Look up existing individual by PIN hash
        var individual = _kycDbContext.Individuals
            .FirstOrDefault(i => i.NationalIdHash == pinHash);

        // 3. Cache-first check — controlled by config toggle
        bool allowCachedMatch = bool.TryParse(
            _configuration["Verification:Intermediate:AllowCachedMatch"], out var acm) && acm;

        if (allowCachedMatch)
        {
            var cacheRetention = _configDbContext.CacheRetentionPolicies
                .FirstOrDefault(c => c.DataType == DataType.VERIFICATION_EVENT);
            int freshnessHours = cacheRetention?.FreshnessUnit == FreshnessUnit.HOURS
                ? cacheRetention.FreshnessValue : 24;
            var cutoff = DateTimeOffset.UtcNow.AddHours(-freshnessHours);

            var cached = _kycDbContext.NrbVerificationEvents
                .Where(e => e.PinSubmittedHash == pinHash
                         && e.Tier == NrbTier.INTERMEDIATE
                         && e.ResponseStatus == "IDENTITY_VERIFIED"
                         && e.ResponseTimestamp >= cutoff)
                .OrderByDescending(e => e.ResponseTimestamp)
                .FirstOrDefault();

            if (cached != null)
            {
                _logger.LogInformation("Serving Intermediate from CACHE for PIN {Hash}", pinHash);
                var gw = new GatewayRequest
                {
                    Id = Guid.NewGuid(), SubsidiaryId = subsidiaryId,
                    IndividualId = individual?.Id, ServedFrom = ServedFrom.CACHE,
                    NrbVerificationEventId = cached.Id, ResponseStatus = cached.ResponseStatus,
                    RequestTimestamp = requestTimestamp
                };
                _kycDbContext.Add(gw);
                await _kycDbContext.SaveChangesAsync(cancellationToken);
                return new IntermediateVerificationResultDto(gw.Id, request.NationalId, true,
                    cached.ResponseStatus, cached.ConfirmationToken, ServedFrom.CACHE, requestTimestamp);
            }
        }

        // 3b. Simulation mode: biometric match simulated against local mirror
        if (IsSimulationMode())
        {
            var simTs = DateTimeOffset.UtcNow;
            if (individual == null)
            {
                _logger.LogWarning("SIMULATION: Intermediate PIN {Hash} not found in local mirror.", pinHash);
                var simNfGw = PersistGatewayRequest(subsidiaryId, null, ServedFrom.CACHE, null,
                    "INVALID_PIN", requestTimestamp);
                await _kycDbContext.SaveChangesAsync(cancellationToken);
                return new IntermediateVerificationResultDto(simNfGw.Id, request.NationalId, false,
                    "INVALID_PIN", null, ServedFrom.CACHE, requestTimestamp);
            }

            _logger.LogInformation("SIMULATION: Intermediate biometric match for PIN {Hash} → MATCH.", pinHash);
            var simEvt = PersistVerificationEvent(individual.Id, pinHash, NrbTier.INTERMEDIATE, subsidiaryShortCode,
                requestTimestamp, simTs, "IDENTITY_VERIFIED", $"SIM_CONF_{Guid.NewGuid():N}", null);
            PersistFieldVerification(individual.Id, "biometric_match", "MATCH",
                VerificationSource.NRB_INTERMEDIATE, VerificationFieldStatus.CORRECT, simTs);
            var simGw = PersistGatewayRequest(subsidiaryId, individual.Id, ServedFrom.CACHE, simEvt.Id,
                "IDENTITY_VERIFIED", requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new IntermediateVerificationResultDto(simGw.Id, request.NationalId, true,
                "IDENTITY_VERIFIED", simEvt.ConfirmationToken, ServedFrom.CACHE, requestTimestamp);
        }

        // 4. Cache miss (or disabled) — call NRB live
        _logger.LogInformation("Calling NRB Intermediate live for PIN {Hash}", pinHash);
        var nrbResp = await _nrbTierAdapter.VerifyIntermediateAsync(
            new NrbIntermediateRequestModel(request.NationalId, request.BiometricBlob, subsidiaryShortCode),
            cancellationToken);
        var responseTimestamp = DateTimeOffset.UtcNow;

        // 4a. PIN not found in registry → do NOT create a mirror record
        bool pinNotFound = nrbResp.Status is "INVALID_PIN" or "NOT_FOUND" or "PIN_NOT_FOUND";
        if (pinNotFound && individual == null)
        {
            _logger.LogWarning("NRB Intermediate: PIN {Hash} not found in registry.", pinHash);
            var nfGw = PersistGatewayRequest(subsidiaryId, null, ServedFrom.NRB, null,
                nrbResp.Status, requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new IntermediateVerificationResultDto(nfGw.Id, request.NationalId, false,
                nrbResp.Status, null, ServedFrom.NRB, requestTimestamp);
        }

        // 5. Ensure individual record exists
        individual ??= await EnsureIndividualAsync(pinHash, request.NationalId, "PENDING_VERIFICATION", "PENDING_VERIFICATION",
            nrbResp.IsMatch ? RecordStatus.PARTIALLY_VERIFIED : RecordStatus.UNVERIFIED, requestTimestamp, responseTimestamp);

        // 6. Persist event + field verification + gateway request
        var evt = PersistVerificationEvent(individual.Id, pinHash, NrbTier.INTERMEDIATE, subsidiaryShortCode,
            requestTimestamp, responseTimestamp, nrbResp.Status, nrbResp.ConfirmationToken, nrbResp.RawResponsePayload);
        PersistFieldVerification(individual.Id, "biometric_match", nrbResp.IsMatch ? "MATCH" : "NO_MATCH",
            VerificationSource.NRB_INTERMEDIATE, nrbResp.IsMatch ? VerificationFieldStatus.CORRECT : VerificationFieldStatus.INCORRECT, responseTimestamp);
        var gwReq = PersistGatewayRequest(subsidiaryId, individual.Id, ServedFrom.NRB, evt.Id, nrbResp.Status, requestTimestamp);

        await _kycDbContext.SaveChangesAsync(cancellationToken);
        return new IntermediateVerificationResultDto(gwReq.Id, request.NationalId, nrbResp.IsMatch,
            nrbResp.Status, nrbResp.ConfirmationToken, ServedFrom.NRB, requestTimestamp);
    }

    // ═══════════════════════════════════════════════════════════════════
    // BASIC (Tier 1) — Always-live field reconciliation, no cache bypass
    // ═══════════════════════════════════════════════════════════════════

    public async Task<BasicVerificationResultDto> VerifyBasicAsync(
        Guid subsidiaryId,
        string subsidiaryShortCode,
        BasicVerificationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var requestTimestamp = DateTimeOffset.UtcNow;
        var pinHash = _hmacService.ComputeHmacSha256(request.IdNumber);

        // 1. Verify tier toggle
        var tierSetting = _configDbContext.VerificationTierSettings
            .FirstOrDefault(t => t.Tier == NrbTier.BASIC);
        if (tierSetting != null && !tierSetting.Enabled)
            throw new InvalidOperationException("The BASIC NRB verification tier is currently disabled.");

        // 2. Look up existing individual
        var individual = _kycDbContext.Individuals
            .FirstOrDefault(i => i.NationalIdHash == pinHash);

        // 3. Simulation mode: compare against local DB instead of calling NRB
        bool simulationMode = bool.TryParse(
            _configuration["Nrb:SimulationMode"], out var sim) && sim;

        NrbBasicResponseModel nrbResp;
        var responseTimestamp = DateTimeOffset.UtcNow;

        if (simulationMode)
        {
            _logger.LogInformation("SIMULATION: Comparing submitted fields against local DB for PIN {Hash}", pinHash);
            nrbResp = SimulateBasicVerification(request, individual, pinHash);
        }
        else
        {
            _logger.LogInformation("Calling NRB Basic live for PIN {Hash} (always-live tier)", pinHash);
            var nrbReq = new NrbBasicRequestModel(
                request.IdNumber, request.Surname, request.FirstName, request.OtherNames,
                request.Nationality, request.Gender, request.DateOfBirthString,
                request.DateOfIssueString, request.DateOfExpiryString, request.PlaceOfBirthDistrictName);
            nrbResp = await _nrbTierAdapter.VerifyBasicAsync(nrbReq, cancellationToken);
        }

        // 4. Handle card status
        RecordStatus recordStatus;
        if (NrbBasicCardStatus.IsRejected(nrbResp.CardStatus))
            recordStatus = RecordStatus.UNVERIFIED;
        else if (NrbBasicCardStatus.RequiresManualReview(nrbResp.CardStatus))
            recordStatus = RecordStatus.NEEDS_CORRECTION;
        else
            recordStatus = RecordStatus.VERIFIED;

        // 4a. NOT FOUND + no existing record → do NOT create a mirror record
        if (string.Equals(nrbResp.CardStatus, NrbBasicCardStatus.NotFound, StringComparison.OrdinalIgnoreCase)
            && individual == null)
        {
            _logger.LogWarning("NRB Basic: PIN {Hash} not found in registry.", pinHash);
            var nfGw = PersistGatewayRequest(subsidiaryId, null, ServedFrom.NRB, null,
                NrbBasicCardStatus.NotFound, requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new BasicVerificationResultDto(nfGw.Id, request.IdNumber, nrbResp.CardStatus,
                nrbResp.FieldResults, ServedFrom.NRB, requestTimestamp);
        }

        // 5. Ensure individual record (create if first time)
        individual ??= await EnsureIndividualAsync(pinHash, request.IdNumber,
            request.FirstName, request.Surname, recordStatus, requestTimestamp, responseTimestamp);
        // Update status if existing record
        if (individual.RecordStatus != recordStatus)
        {
            individual.RecordStatus = recordStatus;
            individual.UpdatedAt = responseTimestamp;
        }

        // 6. Persist per-field verification results (CORRECT/INCORRECT)
        foreach (var (fieldName, result) in nrbResp.FieldResults)
        {
            PersistFieldVerification(individual.Id, fieldName, result,
                VerificationSource.NRB_BASIC,
                result == "CORRECT" ? VerificationFieldStatus.CORRECT : VerificationFieldStatus.INCORRECT,
                responseTimestamp);
        }

        // 7. Persist NRB verification event + gateway request
        var evt = PersistVerificationEvent(individual.Id, pinHash, NrbTier.BASIC, subsidiaryShortCode,
            requestTimestamp, responseTimestamp, nrbResp.CardStatus, null, null);
        var gwReq = PersistGatewayRequest(subsidiaryId, individual.Id, ServedFrom.NRB, evt.Id,
            nrbResp.CardStatus, requestTimestamp);

        await _kycDbContext.SaveChangesAsync(cancellationToken);
        return new BasicVerificationResultDto(gwReq.Id, request.IdNumber, nrbResp.CardStatus,
            nrbResp.FieldResults, ServedFrom.NRB, requestTimestamp);
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEXT LOOKUP (Tier 2) — Demographic retrieval, cache-first
    // ═══════════════════════════════════════════════════════════════════

    public async Task<TextLookupResultDto> TextLookupAsync(
        Guid subsidiaryId,
        string subsidiaryShortCode,
        TextLookupRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var requestTimestamp = DateTimeOffset.UtcNow;
        var pinHash = _hmacService.ComputeHmacSha256(request.IdNumber);

        // 1. Verify tier toggle
        var tierSetting = _configDbContext.VerificationTierSettings
            .FirstOrDefault(t => t.Tier == NrbTier.TEXT_LOOKUP);
        if (tierSetting != null && !tierSetting.Enabled)
            throw new InvalidOperationException("The TEXT_LOOKUP NRB verification tier is currently disabled.");

        // 2. Look up existing individual
        var individual = _kycDbContext.Individuals
            .FirstOrDefault(i => i.NationalIdHash == pinHash);

        // 3. Cache-first: demographics don't change frequently
        var bioRetention = _configDbContext.CacheRetentionPolicies
            .FirstOrDefault(c => c.DataType == DataType.BIOGRAPHIC_RECORD);
        int freshnessHours = bioRetention?.FreshnessUnit == FreshnessUnit.HOURS
            ? bioRetention.FreshnessValue : 720; // Default 30 days
        var cutoff = DateTimeOffset.UtcNow.AddHours(-freshnessHours);

        var cached = _kycDbContext.NrbVerificationEvents
            .Where(e => e.PinSubmittedHash == pinHash
                     && e.Tier == NrbTier.TEXT_LOOKUP
                     && e.ResponseStatus == "IDENTITY_VERIFIED"
                     && e.ResponseTimestamp >= cutoff)
            .OrderByDescending(e => e.ResponseTimestamp)
            .FirstOrDefault();

        if (cached != null && individual != null)
        {
            _logger.LogInformation("Serving Text Lookup from CACHE for PIN {Hash}", pinHash);
            var gw = new GatewayRequest
            {
                Id = Guid.NewGuid(), SubsidiaryId = subsidiaryId,
                IndividualId = individual.Id, ServedFrom = ServedFrom.CACHE,
                NrbVerificationEventId = cached.Id, ResponseStatus = cached.ResponseStatus,
                RequestTimestamp = requestTimestamp
            };
            _kycDbContext.Add(gw);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new TextLookupResultDto(gw.Id, request.IdNumber,
                individual.Surname, individual.FirstName, individual.OtherNames,
                individual.DateOfBirth, individual.Gender.ToString(),
                individual.PhotoRef, individual.FingerprintRef,
                ServedFrom.CACHE, true, requestTimestamp);
        }

        // 3b. Simulation mode: serve from local mirror DB instead of calling NRB
        if (IsSimulationMode())
        {
            if (individual != null)
            {
                _logger.LogInformation("SIMULATION: Serving Text Lookup from local mirror for PIN {Hash}", pinHash);
                var simGw = PersistGatewayRequest(subsidiaryId, individual.Id, ServedFrom.CACHE, null,
                    "IDENTITY_VERIFIED", requestTimestamp);
                await _kycDbContext.SaveChangesAsync(cancellationToken);
                return new TextLookupResultDto(simGw.Id, request.IdNumber,
                    individual.Surname, individual.FirstName, individual.OtherNames,
                    individual.DateOfBirth, individual.Gender.ToString(),
                    individual.PhotoRef, individual.FingerprintRef,
                    ServedFrom.CACHE, true, requestTimestamp);
            }

            _logger.LogWarning("SIMULATION: PIN {Hash} not found in local mirror.", pinHash);
            var simNfGw = PersistGatewayRequest(subsidiaryId, null, ServedFrom.CACHE, null,
                NrbBasicCardStatus.NotFound, requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new TextLookupResultDto(simNfGw.Id, request.IdNumber,
                "", "", null, DateOnly.MinValue, "", null, null,
                ServedFrom.CACHE, false, requestTimestamp);
        }

        // 4. Cache miss — call NRB live
        _logger.LogInformation("Calling NRB Text Lookup live for PIN {Hash}", pinHash);
        var nrbResp = await _nrbTierAdapter.TextLookupAsync(
            new NrbTextLookupRequestModel(request.IdNumber), cancellationToken);
        var responseTimestamp = DateTimeOffset.UtcNow;

        // 4a. NOT FOUND — do NOT create a mirror record for a non-existent person
        if (!nrbResp.IsFound)
        {
            _logger.LogWarning("NRB Text Lookup: PIN {Hash} not found in registry.", pinHash);
            var nfGw = PersistGatewayRequest(subsidiaryId, null, ServedFrom.NRB, null,
                NrbBasicCardStatus.NotFound, requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new TextLookupResultDto(nfGw.Id, request.IdNumber,
                "", "", null, DateOnly.MinValue, "", null, null,
                ServedFrom.NRB, false, requestTimestamp);
        }

        // 5. Persist blobs → blob storage, store refs only in DB
        string? photoRef = null, fingerprintRef = null;
        if (!string.IsNullOrEmpty(nrbResp.PhotoBase64))
            photoRef = await StoreBlobAsync(pinHash, "photo", nrbResp.PhotoBase64);
        if (!string.IsNullOrEmpty(nrbResp.FingerprintBase64))
            fingerprintRef = await StoreBlobAsync(pinHash, "fingerprint", nrbResp.FingerprintBase64);

        // 6. Ensure individual record with NRB demographic data
        bool isNewIndividual = individual == null;
        if (isNewIndividual)
        {
            individual = new Individual { Id = Guid.NewGuid(), NationalIdHash = pinHash,
                NationalIdEncrypted = _encryptionService.Encrypt(request.IdNumber), CreatedAt = requestTimestamp };
            _kycDbContext.Add(individual);
        }
        individual.Surname = nrbResp.Surname;
        individual.FirstName = nrbResp.FirstName;
        individual.OtherNames = nrbResp.OtherNames;
        individual.DateOfBirth = nrbResp.DateOfBirth;
        individual.Gender = Enum.TryParse<Gender>(nrbResp.Gender, true, out var g) ? g : Gender.MALE;
        individual.MaritalStatus = nrbResp.MaritalStatus;
        individual.BirthDistrict = nrbResp.BirthDistrict;
        individual.ResidentialAddress = nrbResp.ResidentialAddress;
        individual.IssueDate = nrbResp.IssueDate;
        individual.ExpiryDate = nrbResp.ExpiryDate;
        individual.TelephoneNumber = nrbResp.TelephoneNumber;
        individual.CardStatus = nrbResp.CardStatus;
        individual.PhotoRef = photoRef ?? individual.PhotoRef;
        individual.FingerprintRef = fingerprintRef ?? individual.FingerprintRef;
        individual.FingerPosition = nrbResp.FingerPosition ?? individual.FingerPosition;
        individual.RecordStatus = RecordStatus.PARTIALLY_VERIFIED;
        individual.UpdatedAt = responseTimestamp;

        // 7. Persist event + gateway request
        var evt = PersistVerificationEvent(individual.Id, pinHash, NrbTier.TEXT_LOOKUP, subsidiaryShortCode,
            requestTimestamp, responseTimestamp, "IDENTITY_VERIFIED", null, null);
        var gwReq = PersistGatewayRequest(subsidiaryId, individual.Id, ServedFrom.NRB, evt.Id,
            "IDENTITY_VERIFIED", requestTimestamp);

        await _kycDbContext.SaveChangesAsync(cancellationToken);
        return new TextLookupResultDto(gwReq.Id, request.IdNumber,
            nrbResp.Surname, nrbResp.FirstName, nrbResp.OtherNames,
            nrbResp.DateOfBirth, nrbResp.Gender, photoRef, fingerprintRef,
            ServedFrom.NRB, true, requestTimestamp);
    }

    // ═══════════════════════════════════════════════════════════════════
    // ADVANCED (Tier 4) — Biometric + OTP, two-phase, always live
    // ═══════════════════════════════════════════════════════════════════

    public async Task<AdvancedVerificationResultDto> VerifyAdvancedAsync(
        Guid subsidiaryId,
        string subsidiaryShortCode,
        AdvancedVerificationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var requestTimestamp = DateTimeOffset.UtcNow;
        var pinHash = _hmacService.ComputeHmacSha256(request.NationalId);

        // 1. Verify tier toggle
        var tierSetting = _configDbContext.VerificationTierSettings
            .FirstOrDefault(t => t.Tier == NrbTier.ADVANCED);
        if (tierSetting != null && !tierSetting.Enabled)
            throw new InvalidOperationException("The ADVANCED NRB verification tier is currently disabled.");

        // 2. Look up existing individual
        var individual = _kycDbContext.Individuals
            .FirstOrDefault(i => i.NationalIdHash == pinHash);

        // 3. No cache for Advanced — OTP-based, always live
        _logger.LogInformation("Calling NRB Advanced live for PIN {Hash}", pinHash);

        // 3a. Simulation mode: simulate two-phase OTP flow without NRB
        if (IsSimulationMode())
        {
            var simTs = DateTimeOffset.UtcNow;
            bool simIsPhase1 = string.IsNullOrEmpty(request.Otp);

            if (simIsPhase1)
            {
                var p1Gw = PersistGatewayRequest(subsidiaryId, individual?.Id, ServedFrom.CACHE, null,
                    "OTP_SENT", requestTimestamp);
                await _kycDbContext.SaveChangesAsync(cancellationToken);
                return new AdvancedVerificationResultDto(p1Gw.Id, request.NationalId,
                    true, "088****234", null, NrbAdvancedPhase.OtpSent, requestTimestamp);
            }

            // Phase 2: complete verification
            individual ??= await EnsureIndividualAsync(pinHash, request.NationalId,
                "PENDING_VERIFICATION", "PENDING_VERIFICATION",
                RecordStatus.VERIFIED, requestTimestamp, simTs);
            individual.RecordStatus = RecordStatus.VERIFIED;
            individual.UpdatedAt = simTs;

            var simEvt = PersistVerificationEvent(individual.Id, pinHash, NrbTier.ADVANCED, subsidiaryShortCode,
                requestTimestamp, simTs, "IDENTITY_VERIFIED", $"SIM_ADV_{Guid.NewGuid():N}", null);
            PersistFieldVerification(individual.Id, "biometric_otp_match", "MATCH",
                VerificationSource.NRB_ADVANCED, VerificationFieldStatus.CORRECT, simTs);
            var p2Gw = PersistGatewayRequest(subsidiaryId, individual.Id, ServedFrom.CACHE, simEvt.Id,
                "IDENTITY_VERIFIED", requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new AdvancedVerificationResultDto(p2Gw.Id, request.NationalId,
                true, "088****234", simEvt.ConfirmationToken, NrbAdvancedPhase.VerificationComplete, requestTimestamp);
        }

        var nrbReq = new NrbAdvancedRequestModel(request.NationalId, request.BiometricBlob, request.Otp);
        var nrbResp = await _nrbTierAdapter.VerifyAdvancedAsync(nrbReq, cancellationToken);
        var responseTimestamp = DateTimeOffset.UtcNow;

        bool isPhase1 = nrbResp.Phase == NrbAdvancedPhase.OtpSent;

        // 3a. Failed + no existing record → do NOT create a mirror record
        if (!nrbResp.IsSuccess && individual == null)
        {
            _logger.LogWarning("NRB Advanced: PIN {Hash} failed OTP/bio and has no local record.", pinHash);
            var nfGw = PersistGatewayRequest(subsidiaryId, null, ServedFrom.NRB, null,
                "VERIFICATION_FAILED", requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new AdvancedVerificationResultDto(nfGw.Id, request.NationalId,
                false, nrbResp.MaskedMobile, null, nrbResp.Phase, requestTimestamp);
        }

        // 4. Ensure individual record
        individual ??= await EnsureIndividualAsync(pinHash, request.NationalId,
            "PENDING_VERIFICATION", "PENDING_VERIFICATION",
            RecordStatus.UNVERIFIED, requestTimestamp, responseTimestamp);

        // 5. Phase 1 (OTP_SENT): log request but do NOT mark as verified
        if (isPhase1)
        {
            var gwReq = PersistGatewayRequest(subsidiaryId, individual.Id, ServedFrom.NRB, null,
                "OTP_SENT", requestTimestamp);
            await _kycDbContext.SaveChangesAsync(cancellationToken);
            return new AdvancedVerificationResultDto(gwReq.Id, request.NationalId,
                nrbResp.IsSuccess, nrbResp.MaskedMobile, null,
                NrbAdvancedPhase.OtpSent, requestTimestamp);
        }

        // 6. Phase 2 (VERIFICATION_COMPLETE): full verification
        individual.RecordStatus = RecordStatus.VERIFIED;
        individual.UpdatedAt = responseTimestamp;

        var evt = PersistVerificationEvent(individual.Id, pinHash, NrbTier.ADVANCED, subsidiaryShortCode,
            requestTimestamp, responseTimestamp, "IDENTITY_VERIFIED", nrbResp.ConfirmationToken, null);
        PersistFieldVerification(individual.Id, "biometric_otp_match", "MATCH",
            VerificationSource.NRB_ADVANCED, VerificationFieldStatus.CORRECT, responseTimestamp);
        var gwReq2 = PersistGatewayRequest(subsidiaryId, individual.Id, ServedFrom.NRB, evt.Id,
            "IDENTITY_VERIFIED", requestTimestamp);

        await _kycDbContext.SaveChangesAsync(cancellationToken);
        return new AdvancedVerificationResultDto(gwReq2.Id, request.NationalId,
            nrbResp.IsSuccess, nrbResp.MaskedMobile, nrbResp.ConfirmationToken,
            NrbAdvancedPhase.VerificationComplete, requestTimestamp);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Shared helpers
    // ═══════════════════════════════════════════════════════════════════

    private bool IsSimulationMode() =>
        bool.TryParse(_configuration["Nrb:SimulationMode"], out var sim) && sim;

    private async Task<Individual> EnsureIndividualAsync(string pinHash, string nationalId,
        string firstName, string surname, RecordStatus status,
        DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        var ind = new Individual
        {
            Id = Guid.NewGuid(),
            NationalIdHash = pinHash,
            NationalIdEncrypted = _encryptionService.Encrypt(nationalId),
            FirstName = firstName,
            Surname = surname,
            RecordStatus = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        _kycDbContext.Add(ind);
        return ind;
    }

    private NrbVerificationEvent PersistVerificationEvent(Guid individualId, string pinHash,
        NrbTier tier, string subsidiary, DateTimeOffset reqTs, DateTimeOffset respTs,
        string status, string? confirmationToken, string? rawPayload)
    {
        var evt = new NrbVerificationEvent
        {
            Id = Guid.NewGuid(), IndividualId = individualId,
            PinSubmittedHash = pinHash, Tier = tier,
            RequestingSubsidiary = subsidiary,
            RequestTimestamp = reqTs, ResponseTimestamp = respTs,
            ResponseStatus = status, ConfirmationToken = confirmationToken,
            RawResponseRef = rawPayload
        };
        _kycDbContext.Add(evt);
        return evt;
    }

    private void PersistFieldVerification(Guid individualId, string fieldName, string value,
        VerificationSource source, VerificationFieldStatus status, DateTimeOffset verifiedAt)
    {
        _kycDbContext.Add(new IndividualFieldVerification
        {
            Id = Guid.NewGuid(), IndividualId = individualId,
            FieldName = fieldName, Value = value,
            Source = source, VerificationStatus = status,
            VerifiedAt = verifiedAt, Superseded = false
        });
    }

    private GatewayRequest PersistGatewayRequest(Guid subsidiaryId, Guid? individualId,
        ServedFrom servedFrom, Guid? eventId, string status, DateTimeOffset timestamp)
    {
        var gw = new GatewayRequest
        {
            Id = Guid.NewGuid(), SubsidiaryId = subsidiaryId,
            IndividualId = individualId, ServedFrom = servedFrom,
            NrbVerificationEventId = eventId, ResponseStatus = status,
            RequestTimestamp = timestamp
        };
        _kycDbContext.Add(gw);
        return gw;
    }

    /// <summary>
    /// Stores a base64 blob and returns a reference pointer.
    /// TODO: Replace with real blob storage (Azure Blob / S3) in production.
    /// Currently writes to a local directory for dev purposes only.
    /// </summary>
    private async Task<string?> StoreBlobAsync(string pinHash, string blobType, string base64Data)
    {
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "chl_nrb_blobs");
            Directory.CreateDirectory(dir);
            var fileName = $"{pinHash}_{blobType}_{Guid.NewGuid():N}.bin";
            var path = Path.Combine(dir, fileName);
            var bytes = Convert.FromBase64String(base64Data);
            await File.WriteAllBytesAsync(path, bytes);
            _logger.LogInformation("Blob stored: {Type} → {Path}", blobType, path);
            return path; // In production this would be a blob storage URI
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store {Type} blob for PIN {Hash}", blobType, pinHash);
            return null;
        }
    }

    /// <summary>
    /// Simulation mode: compares submitted Basic verification fields against
    /// the locally-stored individual record. Returns CORRECT/INCORRECT per field.
    /// Used when Nrb:SimulationMode = true (no real NRB connection).
    /// </summary>
    private NrbBasicResponseModel SimulateBasicVerification(
        BasicVerificationRequestDto request,
        Individual? individual,
        string pinHash)
    {
        if (individual == null)
        {
            _logger.LogInformation("SIMULATION: No local record for PIN {Hash} — NOT FOUND.", pinHash);
            return new NrbBasicResponseModel(NrbBasicCardStatus.NotFound,
                new Dictionary<string, string> { ["IdNumber"] = "INCORRECT" });
        }

        var f = new Dictionary<string, string>
        {
            ["IdNumber"] = request.IdNumber == _encryptionService.Decrypt(individual.NationalIdEncrypted ?? "") ? "CORRECT" : "INCORRECT",
            ["Surname"] = string.Equals(request.Surname, individual.Surname, StringComparison.OrdinalIgnoreCase) ? "CORRECT" : "INCORRECT",
            ["FirstName"] = string.Equals(request.FirstName, individual.FirstName, StringComparison.OrdinalIgnoreCase) ? "CORRECT" : "INCORRECT",
            ["OtherNames"] = string.Equals(request.OtherNames ?? "", individual.OtherNames ?? "", StringComparison.OrdinalIgnoreCase) ? "CORRECT" : "INCORRECT",
            ["Gender"] = string.Equals(request.Gender, individual.Gender.ToString(), StringComparison.OrdinalIgnoreCase) ? "CORRECT" : "INCORRECT",
            ["DateOfBirth"] = request.DateOfBirthString == individual.DateOfBirth.ToString("yyyy-MM-dd") ? "CORRECT" : "INCORRECT",
            ["DateOfIssue"] = "CORRECT",
            ["DateOfExpiry"] = "CORRECT",
            ["PlaceOfBirthDistrict"] = string.Equals(request.PlaceOfBirthDistrictName ?? "", individual.BirthDistrict ?? "", StringComparison.OrdinalIgnoreCase) ? "CORRECT" : "INCORRECT"
        };

        string cardStatus = individual.CardStatus ?? NrbBasicCardStatus.Valid;

        _logger.LogInformation("SIMULATION: {FieldCount} fields compared. Card: {Status}", f.Count, cardStatus);
        return new NrbBasicResponseModel(cardStatus, f);
    }

    // ═══════════════════════════════════════════════════════════════════
    // REVALIDATION — Admin-triggered batch re-check of local mirror vs NRB
    // ═══════════════════════════════════════════════════════════════════

    public async Task<RevalidationResultDto> RevalidateAllAsync(
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        int total = 0, valid = 0, expired = 0, deceased = 0, seeNrb = 0, errors = 0;

        var individuals = _kycDbContext.Individuals.ToList();
        total = individuals.Count;

        _logger.LogInformation("REVALIDATION: Checking {Count} PINs against NRB Basic tier.", total);

        foreach (var ind in individuals)
        {
            try
            {
                var nationalId = _encryptionService.Decrypt(ind.NationalIdEncrypted ?? "");
                if (string.IsNullOrEmpty(nationalId)) { errors++; continue; }

                var nrbReq = new NrbBasicRequestModel(
                    nationalId, ind.Surname, ind.FirstName, ind.OtherNames,
                    "", ind.Gender.ToString(), ind.DateOfBirth.ToString("yyyy-MM-dd"),
                    ind.IssueDate?.ToString("yyyy-MM-dd"), ind.ExpiryDate?.ToString("yyyy-MM-dd"),
                    ind.BirthDistrict);

                var nrbResp = await _nrbTierAdapter.VerifyBasicAsync(nrbReq, cancellationToken);

                ind.CardStatus = nrbResp.CardStatus;
                ind.LastRevalidatedAt = DateTimeOffset.UtcNow;
                ind.UpdatedAt = DateTimeOffset.UtcNow;

                if (NrbBasicCardStatus.IsRejected(nrbResp.CardStatus))
                    ind.RecordStatus = RecordStatus.UNVERIFIED;
                else if (NrbBasicCardStatus.IsStale(nrbResp.CardStatus))
                    ind.RecordStatus = RecordStatus.NEEDS_CORRECTION;
                else
                    ind.RecordStatus = RecordStatus.VERIFIED;

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
                _logger.LogError(ex, "REVALIDATION: Failed for PIN hash {Hash}", ind.NationalIdHash);
                errors++;
            }
        }

        await _kycDbContext.SaveChangesAsync(cancellationToken);
        var completedAt = DateTimeOffset.UtcNow;

        // Audit log in config schema
        _configDbContext.Add(new ConfigAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            SettingArea = SettingArea.NRB_ENVIRONMENT,
            SettingKey = "revalidation.batch",
            OldValue = null,
            NewValue = $"Checked {total}: {valid} valid, {expired} expired, {deceased} deceased, {seeNrb} see NRB, {errors} errors",
            ChangedAt = completedAt
        });
        await _configDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("REVALIDATION complete. Total={Total}, Valid={Valid}, Expired={Expired}, Deceased={Deceased}, SeeNrb={SeeNrb}, Errors={Errors}",
            total, valid, expired, deceased, seeNrb, errors);

        return new RevalidationResultDto(total, valid, expired, deceased, seeNrb, errors, startedAt, completedAt);
    }
}
