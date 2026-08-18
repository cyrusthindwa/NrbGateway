using CHL.NrbGateway.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace CHL.NrbGateway.Infrastructure.Services;

/// <summary>
/// MinIO (S3-compatible) implementation of the blob storage abstraction.
/// The configured bucket is created lazily on first use.
/// </summary>
public class MinioBlobStorageService : IBlobStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucket;
    private readonly ILogger<MinioBlobStorageService> _logger;

    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketEnsured;

    public MinioBlobStorageService(IConfiguration configuration, ILogger<MinioBlobStorageService> logger)
    {
        _logger = logger;

        var endpoint = configuration["BlobStorage:Endpoint"] ?? "localhost:9000";
        var accessKey = configuration["BlobStorage:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["BlobStorage:SecretKey"] ?? "minioadmin";
        var useSsl = bool.TryParse(configuration["BlobStorage:UseSsl"], out var ssl) && ssl;
        _bucket = configuration["BlobStorage:Bucket"] ?? "nrb-documents";

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSsl)
            .Build();
    }

    public async Task<string?> StoreAsync(
        string subjectKey,
        string documentType,
        string? blobFormat,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureBucketAsync(cancellationToken);

            var extension = string.IsNullOrWhiteSpace(blobFormat)
                ? "bin"
                : blobFormat.ToLowerInvariant();
            var objectName = $"{subjectKey}/{documentType}/{Guid.NewGuid():N}.{extension}";

            using var stream = new MemoryStream(data);
            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucket)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(data.Length)
                .WithContentType("application/octet-stream"), cancellationToken);

            return objectName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store {DocumentType} blob for subject {SubjectKey}", documentType, subjectKey);
            return null;
        }
    }

    public async Task<Stream?> GetAsync(string blobRef, CancellationToken cancellationToken = default)
    {
        try
        {
            var output = new MemoryStream();
            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_bucket)
                .WithObject(blobRef)
                .WithCallbackStream(s => s.CopyTo(output)), cancellationToken);
            output.Position = 0;
            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve blob {BlobRef}", blobRef);
            return null;
        }
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketEnsured) return;

        await _bucketLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketEnsured) return;

            var exists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucket), cancellationToken);
            if (!exists)
            {
                await _minioClient.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucket), cancellationToken);
            }

            _bucketEnsured = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }
}
