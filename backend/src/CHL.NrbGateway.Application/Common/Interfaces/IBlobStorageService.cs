namespace CHL.NrbGateway.Application.Common.Interfaces;

/// <summary>
/// Object storage abstraction for NRB document blobs (photos, fingerprints,
/// signatures). Backed by MinIO (S3-compatible). Callers only ever persist and
/// read references (blob_ref) — never inline bytes.
/// </summary>
public interface IBlobStorageService
{
    Task<string?> StoreAsync(
        string subjectKey,
        string documentType,
        string? blobFormat,
        byte[] data,
        CancellationToken cancellationToken = default);

    Task<Stream?> GetAsync(string blobRef, CancellationToken cancellationToken = default);
}
