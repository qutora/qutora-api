using Microsoft.Extensions.Logging;
using Qutora.Domain.Entities;
using Qutora.Shared.DTOs;
using System.Security.Cryptography;
using System.Text;
using Qutora.Application.Interfaces.Storage;
using Qutora.Shared.Enums;

namespace Qutora.Infrastructure.Storage.Providers;

/// <summary>
/// Base class for all storage providers.
/// </summary>
public abstract class BaseStorageProvider(string providerId, ILogger logger) : IStorageProvider
{
    protected readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _providerId = providerId ?? throw new ArgumentNullException(nameof(providerId));

    /// <inheritdoc/>
    public string ProviderId => _providerId;

    /// <inheritdoc/>
    public abstract string ProviderType { get; }

    /// <inheritdoc/>
    public abstract Task<string> UploadAsync(string objectKey, Stream content, string contentType);

    /// <inheritdoc/>
    public abstract Task<UploadResult> UploadWithResultAsync(string? objectKey, Stream content, string fileName,
        string documentId, string? contentType = null, string? bucketName = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<Stream> DownloadAsync(string objectKey);

    /// <inheritdoc/>
    public virtual Task<string> GetDownloadUrlAsync(string objectKey, int expiryInSeconds = 3600)
    {
        throw new NotSupportedException($"{ProviderType} storage provider does not support generating download URLs");
    }

    /// <inheritdoc/>
    public abstract Task DeleteAsync(string objectKey);

    /// <inheritdoc/>
    public abstract Task<bool> ExistsAsync(string objectKey);

    /// <inheritdoc/>
    public abstract Task<bool> TestConnectionAsync();

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<BucketInfoDto>> ListBucketsAsync()
    {
        _logger.LogWarning("{ProviderType} provider has not implemented ListBucketsAsync", ProviderType);
        await Task.CompletedTask;
        return [];
    }

    /// <inheritdoc/>
    public virtual async Task<bool> BucketExistsAsync(string bucketName)
    {
        _logger.LogWarning("{ProviderType} provider has not implemented BucketExistsAsync", ProviderType);
        await Task.CompletedTask;
        return false;
    }

    /// <inheritdoc/>
    public virtual async Task<bool> CreateBucketAsync(string bucketName)
    {
        _logger.LogWarning("{ProviderType} provider has not implemented CreateBucketAsync", ProviderType);
        await Task.CompletedTask;
        return false;
    }

    /// <inheritdoc/>
    public virtual async Task<bool> RemoveBucketAsync(string bucketName, bool force = false)
    {
        _logger.LogWarning("{ProviderType} provider has not implemented RemoveBucketAsync", ProviderType);
        await Task.CompletedTask;
        return false;
    }

    public bool SupportsCapability(StorageCapability capability)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public virtual string GetBucketSearchKey(StorageBucket bucket)
    {
        return bucket.Path;
    }

    /// <summary>
    /// Calculates SHA-256 hash for file
    /// </summary>
    public async Task<string> GetHashAsync(Stream content)
    {
        try
        {
            if (content == null || !content.CanRead) throw new ArgumentException("Content stream is not readable");

            long originalPosition = 0;
            if (content.CanSeek)
            {
                originalPosition = content.Position;
                content.Position = 0;
            }

            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(content);

            if (content.CanSeek) content.Position = originalPosition;

            StringBuilder sb = new();
            for (var i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("x2"));

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating file hash");
            throw;
        }
    }

    /// <summary>
    /// Creates object key (standard format)
    /// </summary>
    /// <param name="objectKey">Existing object key (if any)</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="fileName">File name</param>
    /// <param name="bucketName">Bucket name (optional)</param>
    /// <returns>Created object key</returns>
    protected virtual string CreateObjectKey(string? objectKey, string documentId, string fileName,
        string? bucketName = null)
    {
        string objectPath;

        if (string.IsNullOrEmpty(objectKey))
            objectPath = $"{documentId}/{Guid.NewGuid()}-{fileName}";
        else
            objectPath = objectKey;

        return objectPath;
    }

    /// <summary>
    /// Sets content type to a default value (if empty)
    /// </summary>
    /// <param name="contentType">Content type</param>
    /// <returns>Set content type</returns>
    protected string EnsureContentType(string? contentType)
    {
        return string.IsNullOrEmpty(contentType)
            ? "application/octet-stream"
            : contentType;
    }

    /// <summary>
    /// Upload operation debugging
    /// </summary>
    protected void LogUploadSuccess(string objectKey, long contentLength)
    {
        _logger.LogInformation("File uploaded to {ProviderType} storage: {ObjectKey}, Size: {Size}",
            ProviderType, objectKey, contentLength);
    }

    /// <summary>
    /// Information log in case of error
    /// </summary>
    protected void LogError(Exception ex, string message, params object[] args)
    {
        _logger.LogError(ex, message, args);
    }
}
