using System.Security.Cryptography;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Interfaces.Storage;
using Qutora.Shared.DTOs;
using Qutora.Shared.Enums;

namespace Qutora.Infrastructure.Storage.Providers;

/// <summary>
/// Wrapper class for IStorageProviderAdapter -> IStorageProvider conversion
/// </summary>
internal class StorageProviderWrapper : IStorageProvider
{
    private readonly IStorageProviderAdapter _adapter;
    private readonly string _providerType;
    private readonly IStorageCapabilityCache _capabilityCache;

    private static readonly Dictionary<string, HashSet<StorageCapability>> _providerCapabilities =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["filesystem"] =
            [
                StorageCapability.BucketListing,
                StorageCapability.BucketExistence,
                StorageCapability.BucketCreation,
                StorageCapability.BucketDeletion,
                StorageCapability.NestedBuckets,
                StorageCapability.ForceDelete
            ],
            ["minio"] =
            [
                StorageCapability.BucketListing,
                StorageCapability.BucketExistence,
                StorageCapability.BucketCreation,
                StorageCapability.BucketDeletion,
                StorageCapability.ForceDelete,
                StorageCapability.ObjectMetadata,
                StorageCapability.ObjectVersioning
            ],
            ["s3"] =
            [
                StorageCapability.BucketListing,
                StorageCapability.BucketExistence,
                StorageCapability.BucketCreation,
                StorageCapability.BucketDeletion,
                StorageCapability.ForceDelete,
                StorageCapability.ObjectMetadata,
                StorageCapability.ObjectVersioning,
                StorageCapability.ObjectAcl,
                StorageCapability.BucketAcl,
                StorageCapability.BucketLifecycle
            ],
            ["ftp"] =
            [
                StorageCapability.BucketListing,
                StorageCapability.BucketExistence,
                StorageCapability.BucketCreation,
                StorageCapability.BucketDeletion,
                StorageCapability.NestedBuckets,
                StorageCapability.ForceDelete
            ],
            ["sftp"] =
            [
                StorageCapability.BucketListing,
                StorageCapability.BucketExistence,
                StorageCapability.BucketCreation,
                StorageCapability.BucketDeletion,
                StorageCapability.NestedBuckets,
                StorageCapability.ForceDelete
            ]
        };

    public StorageProviderWrapper(IStorageProviderAdapter adapter, string providerType,
        IStorageCapabilityCache capabilityCache)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _providerType = providerType ?? throw new ArgumentNullException(nameof(providerType));
        _capabilityCache = capabilityCache ?? throw new ArgumentNullException(nameof(capabilityCache));
    }

    public string ProviderType => _providerType;

    public string ProviderId =>
        _adapter.GetType().Name + "-" + Guid.NewGuid().ToString("N");

    public async Task<Stream> DownloadAsync(string objectKey)
    {
        return await _adapter.DownloadFileAsync(objectKey);
    }

    public async Task<string> UploadAsync(string objectKey, Stream content, string contentType)
    {
        return await _adapter.UploadFileAsync(content, objectKey, contentType);
    }

    public async Task<UploadResult> UploadWithResultAsync(string? objectKey, Stream content, string fileName,
        string documentId, string? contentType = null, string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var path = await _adapter.UploadFileAsync(content, fileName, contentType, bucketName, cancellationToken);

            content.Position = 0;
            var fileHash = await GetHashAsync(content);

            return new UploadResult
            {
                Success = true,
                StoragePath = path,
                FileId = documentId,
                FileName = fileName,
                ContentType = contentType ?? "application/octet-stream",
                FileSize = content.Length,
                FileHash = fileHash,
                ProviderName = _providerType,
                UploadedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new UploadResult
            {
                Success = false,
                FileName = fileName,
                FileId = documentId,
                ErrorMessage = ex.Message,
                ProviderName = _providerType,
                UploadedAt = DateTime.UtcNow
            };
        }
    }

    public async Task DeleteAsync(string objectKey)
    {
        await _adapter.DeleteFileAsync(objectKey);
    }

    public async Task<bool> ExistsAsync(string objectKey)
    {
        return await _adapter.FileExistsAsync(objectKey);
    }

    public async Task<bool> TestConnectionAsync()
    {
        var result = await _adapter.TestConnectionAsync();
        return result.Success;
    }

    public async Task<string> GetDownloadUrlAsync(string objectKey, int expiryMinutes)
    {
        try
        {
            return await _adapter.GetTemporaryUrlAsync(objectKey, TimeSpan.FromMinutes(expiryMinutes));
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<string> GetHashAsync(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        var startPosition = stream.Position;

        try
        {
            stream.Position = 0;

            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream);

            return Convert.ToBase64String(hash);
        }
        finally
        {
            stream.Position = startPosition;
        }
    }

    /// <summary>
    /// Lists all buckets/folders
    /// </summary>
    public async Task<IEnumerable<BucketInfoDto>> ListBucketsAsync()
    {
        return await _adapter.ListBucketsAsync();
    }

    /// <summary>
    /// Checks if a bucket/folder exists
    /// </summary>
    public async Task<bool> BucketExistsAsync(string bucketName)
    {
        return await _adapter.BucketExistsAsync(bucketName);
    }

    /// <summary>
    /// Creates a new bucket/folder
    /// </summary>
    public async Task<bool> CreateBucketAsync(string bucketName)
    {
        return await _adapter.CreateBucketAsync(bucketName);
    }

    /// <summary>
    /// Deletes a bucket/folder
    /// </summary>
    public async Task<bool> RemoveBucketAsync(string bucketName, bool force = false)
    {
        return await _adapter.RemoveBucketAsync(bucketName, force);
    }

    /// <summary>
    /// Checks if the provider supports the specified capability
    /// </summary>
    public bool SupportsCapability(StorageCapability capability)
    {
        if (_providerCapabilities.TryGetValue(_providerType.ToLowerInvariant(), out var capabilities))
        {
            var hasCapability = capabilities.Contains(capability);

            var cacheKey = _capabilityCache.CreateCacheKey(_adapter.ProviderId);

            var cachedResult = _capabilityCache.GetCachedCapability(cacheKey, capability);
            if (cachedResult.HasValue) return cachedResult.Value;

            return hasCapability;
        }

        return false;
    }

    /// <summary>
    /// Determines provider-specific bucket search key from database bucket information
    /// </summary>
    public string GetBucketSearchKey(StorageBucket bucket)
    {
        if (_adapter is IStorageProvider provider) return provider.GetBucketSearchKey(bucket);

        return bucket.Path;
    }
}
