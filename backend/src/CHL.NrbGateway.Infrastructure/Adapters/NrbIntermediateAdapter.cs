using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.Models;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CHL.NrbGateway.Infrastructure.Adapters;

public class NrbIntermediateAdapter : INrbTierAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NrbIntermediateAdapter> _logger;
    private readonly OAuthAuthProvider _oauthAuth;
    private readonly ClientKeyAuthProvider _clientKeyAuth;

    public NrbTier Tier => NrbTier.INTERMEDIATE;

    public NrbIntermediateAdapter(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<NrbIntermediateAdapter> logger,
        OAuthAuthProvider oauthAuth,
        ClientKeyAuthProvider clientKeyAuth)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _oauthAuth = oauthAuth;
        _clientKeyAuth = clientKeyAuth;
    }

    // ═══════════════════════════════════════════════════════════════════
    // INTERMEDIATE (Tier 3) — Biometric match
    // ═══════════════════════════════════════════════════════════════════

    public async Task<NrbIntermediateResponseModel> VerifyIntermediateAsync(
        NrbIntermediateRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["Nrb:BaseUrl"] ?? "https://nrb-api-test.cict.gov.mw";
        var endpointUrl = $"{baseUrl.TrimEnd('/')}/middleware/iVerify";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
        await _oauthAuth.ApplyAuthAsync(httpRequest, cancellationToken);

        var body = new
        {
            national_id = request.NationalId,
            biometric_blob = request.BiometricBlob,
            requesting_project = request.ProjectCode
        };
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        _logger.LogInformation("NRB Intermediate → POST {Url}", endpointUrl);

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                bool isMatch = root.TryGetProperty("is_match", out var m) && m.GetBoolean();
                string status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "IDENTITY_VERIFIED" : "IDENTITY_VERIFIED";
                string? token = root.TryGetProperty("confirmation_token", out var t) ? t.GetString() : null;

                return new NrbIntermediateResponseModel(isMatch, status, token ?? Guid.NewGuid().ToString("N"), responseBody);
            }

            _logger.LogWarning("NRB Intermediate HTTP {Code}: {Body}", response.StatusCode, responseBody);
            return new NrbIntermediateResponseModel(false, "MATCH_FAILED", null, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NRB Intermediate call failed.");
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // BASIC (Tier 1) — Field reconciliation
    // NRB endpoint: POST {base}/verify/postverify
    // ═══════════════════════════════════════════════════════════════════

    public async Task<NrbBasicResponseModel> VerifyBasicAsync(
        NrbBasicRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["Nrb:BaseUrl"] ?? "https://nrb-api-test.cict.gov.mw";
        var endpointUrl = $"{baseUrl.TrimEnd('/')}/verify/postverify";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
        await _oauthAuth.ApplyAuthAsync(httpRequest, cancellationToken);

        var body = new
        {
            IDNumber = request.IdNumber,
            Surname = request.Surname,
            FirstName = request.FirstName,
            OtherNames = request.OtherNames,
            Nationality = request.Nationality,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirthString,
            DateOfIssue = request.DateOfIssueString,
            DateOfExpiry = request.DateOfExpiryString,
            PlaceOfBirthDistrictName = request.PlaceOfBirthDistrictName
        };
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        _logger.LogInformation("NRB Basic → POST {Url}", endpointUrl);

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                string cardStatus = root.TryGetProperty("CardStatus", out var cs)
                    ? cs.GetString() ?? NrbBasicCardStatus.Invalid
                    : NrbBasicCardStatus.Invalid;

                var fieldResults = new Dictionary<string, string>();
                if (root.TryGetProperty("FieldResults", out var fr) && fr.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in fr.EnumerateObject())
                        fieldResults[prop.Name] = prop.Value.GetString() ?? "INCORRECT";
                }

                return new NrbBasicResponseModel(cardStatus, fieldResults);
            }

            _logger.LogWarning("NRB Basic HTTP {Code}: {Body}", response.StatusCode, responseBody);
            return new NrbBasicResponseModel(NrbBasicCardStatus.NotFound, new Dictionary<string, string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NRB Basic call failed.");
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEXT LOOKUP (Tier 2) — Demographic retrieval
    // NRB endpoint: GET {base}/api/person?IDNumber=...
    // Auth: ClientId + ClientKey headers (not OAuth)
    // ═══════════════════════════════════════════════════════════════════

    public async Task<NrbTextLookupResponseModel> TextLookupAsync(
        NrbTextLookupRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["Nrb:BaseUrl"] ?? "https://nrb-api-test.cict.gov.mw";
        var endpointUrl = $"{baseUrl.TrimEnd('/')}/api/person?IDNumber={Uri.EscapeDataString(request.IdNumber)}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpointUrl);
        await _clientKeyAuth.ApplyAuthAsync(httpRequest, cancellationToken);

        _logger.LogInformation("NRB Text Lookup → GET {Url}", $"{baseUrl.TrimEnd('/')}/api/person");

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                // NRB response: { "Data": { ... }, "Document": { ... } }
                var data = root.TryGetProperty("Data", out var d) ? d : root;
                var document = root.TryGetProperty("Document", out var docEl) ? docEl : default;

                string cardStatus = GetString(data, "Card_status") ?? "VALID";
                string? nid = GetString(data, "Nid");
                string surname = GetString(data, "Surname") ?? "";
                bool isFound = !string.Equals(cardStatus, "NOT FOUND", StringComparison.OrdinalIgnoreCase)
                    && (!string.IsNullOrEmpty(nid) || !string.IsNullOrEmpty(surname));

                return new NrbTextLookupResponseModel(
                    Nid: nid ?? request.IdNumber,
                    FirstName: GetString(data, "First_name") ?? "",
                    OtherNames: GetString(data, "Other_names"),
                    Surname: surname,
                    Gender: GetString(data, "Gender") ?? "",
                    MaritalStatus: GetString(data, "Maritual_status") ?? "",
                    BirthDistrict: GetString(data, "BirthDistrict") ?? "",
                    ResidentialAddress: GetString(data, "Residential_Address") ?? "",
                    DateOfBirth: ParseDate(data, "Date_of_birth"),
                    IssueDate: ParseDate(data, "Issue_date"),
                    ExpiryDate: ParseDate(data, "Expiry_date"),
                    TelephoneNumber: GetString(data, "Telephone_Number"),
                    CardStatus: cardStatus,
                    PhotoBase64: GetString(document, "photo"),
                    FingerprintBase64: GetString(document, "Finger"),
                    FingerPosition: GetString(document, "Fingerposition"),
                    Error: GetString(document, "Error"),
                    ErrorDescription: GetString(document, "Error_description"),
                    IsFound: isFound
                );
            }

            // NRB returned 404 — ID not found in registry
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("NRB Text Lookup: ID {Id} not found (404).", request.IdNumber);
                return new NrbTextLookupResponseModel(
                    Nid: request.IdNumber, FirstName: "", OtherNames: null, Surname: "",
                    Gender: "", MaritalStatus: "", BirthDistrict: "", ResidentialAddress: "",
                    DateOfBirth: DateOnly.MinValue, IssueDate: null, ExpiryDate: null,
                    TelephoneNumber: null, CardStatus: NrbBasicCardStatus.NotFound,
                    PhotoBase64: null, FingerprintBase64: null, FingerPosition: null,
                    Error: "NOT_FOUND", ErrorDescription: "ID not found in NRB registry",
                    IsFound: false
                );
            }

            _logger.LogWarning("NRB Text Lookup HTTP {Code}: {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"NRB Text Lookup returned {response.StatusCode}");
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "NRB Text Lookup call failed.");
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // ADVANCED (Tier 4) — Biometric + OTP, two-phase
    // NRB endpoint: POST {base}/middleware/iVerify (same URL as Intermediate)
    // Phase determined by payload: blob + empty OTP → Phase 1; empty blob + OTP → Phase 2
    // ═══════════════════════════════════════════════════════════════════

    public async Task<NrbAdvancedResponseModel> VerifyAdvancedAsync(
        NrbAdvancedRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["Nrb:BaseUrl"] ?? "https://nrb-api-test.cict.gov.mw";
        var endpointUrl = $"{baseUrl.TrimEnd('/')}/middleware/iVerify";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
        await _oauthAuth.ApplyAuthAsync(httpRequest, cancellationToken);

        var body = new
        {
            national_id = request.NationalId,
            biometric_blob = request.BiometricBlob ?? "",
            otp = request.Otp ?? ""
        };
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        _logger.LogInformation("NRB Advanced → POST {Url}  (blob={HasBlob}, otp={HasOtp})",
            endpointUrl, !string.IsNullOrEmpty(request.BiometricBlob), !string.IsNullOrEmpty(request.Otp));

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                bool isSuccess = root.TryGetProperty("is_success", out var s) && s.GetBoolean();
                string? maskedMobile = root.TryGetProperty("masked_mobile", out var mm) ? mm.GetString() : null;
                string? confirmationToken = root.TryGetProperty("confirmation_token", out var ct) ? ct.GetString() : null;
                string phase = root.TryGetProperty("phase", out var p) ? p.GetString() ?? NrbAdvancedPhase.OtpSent : NrbAdvancedPhase.OtpSent;

                // Branch on the response shape: OTP_SENT vs a direct detailed response.
                var person = ParseAdvancedPerson(root);
                var blobs = ParseAdvancedBlobs(root);
                bool hasDetailedData = person != null || (blobs != null && blobs.Count > 0);

                var responseMode = phase == NrbAdvancedPhase.OtpSent && !hasDetailedData
                    ? ResponseMode.OTP_SENT
                    : (hasDetailedData ? ResponseMode.DETAILED : ResponseMode.OTP_SENT);

                return new NrbAdvancedResponseModel(isSuccess, maskedMobile, confirmationToken, phase,
                    responseMode, person, blobs);
            }

            _logger.LogWarning("NRB Advanced HTTP {Code}: {Body}", response.StatusCode, responseBody);
            return new NrbAdvancedResponseModel(false, null, null, NrbAdvancedPhase.OtpSent,
                ResponseMode.OTP_SENT, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NRB Advanced call failed.");
            throw;
        }
    }

    // ── JSON helpers ──

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
    }

    private static DateOnly ParseDate(JsonElement element, string propertyName)
    {
        var str = GetString(element, propertyName);
        return str != null && DateOnly.TryParse(str, out var d) ? d : DateOnly.MinValue;
    }

    private static NrbAdvancedPersonData? ParseAdvancedPerson(JsonElement root)
    {
        var person = root.TryGetProperty("person", out var p) && p.ValueKind == JsonValueKind.Object
            ? p
            : (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Object ? d : default);

        if (person.ValueKind != JsonValueKind.Object) return null;

        return new NrbAdvancedPersonData(
            GetString(person, "Surname") ?? GetString(person, "surname"),
            GetString(person, "FirstName") ?? GetString(person, "first_name"),
            GetString(person, "OtherNames") ?? GetString(person, "other_names"),
            GetString(person, "Gender") ?? GetString(person, "gender"),
            GetString(person, "CivilStatus") ?? GetString(person, "civil_status"),
            GetString(person, "BirthDistrict") ?? GetString(person, "birth_district"),
            GetString(person, "PlaceOfPermanentResidence") ?? GetString(person, "place_of_permanent_residence"),
            ParseNullableDate(person, "Date_of_birth"),
            ParseNullableDate(person, "Issue_date"),
            ParseNullableDate(person, "Expiry_date"),
            GetString(person, "CardStatus") ?? GetString(person, "card_status"),
            GetString(person, "MiddlewareStatus") ?? GetString(person, "middleware_status")
        );
    }

    private static List<NrbAdvancedBlob>? ParseAdvancedBlobs(JsonElement root)
    {
        if (!root.TryGetProperty("blobs", out var blobs) || blobs.ValueKind != JsonValueKind.Array)
            return null;

        var result = new List<NrbAdvancedBlob>();
        foreach (var b in blobs.EnumerateArray())
        {
            result.Add(new NrbAdvancedBlob(
                GetString(b, "Description") ?? GetString(b, "description"),
                GetString(b, "BlobType") ?? GetString(b, "blob_type"),
                GetString(b, "BlobIndex") ?? GetString(b, "blob_index"),
                GetString(b, "Data") ?? GetString(b, "data")
            ));
        }
        return result;
    }

    private static DateOnly? ParseNullableDate(JsonElement element, string propertyName)
    {
        var str = GetString(element, propertyName);
        return str != null && DateOnly.TryParse(str, out var d) ? d : null;
    }
}
