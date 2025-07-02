using Qutora.Domain.Entities;
using Qutora.Shared.DTOs;
using Qutora.Shared.Enums;

namespace Qutora.Infrastructure.Interfaces.Storage;

/// <summary>
/// Common interface for all storage providers
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// Uploads a file to the storage system
    /// </summary>
    /// <param name="objectKey">Object key or file name</param>
    /// <param name="content">File content stream</param>
    /// <param name="contentType">File content type</param>
    /// <returns>Path of the file created in the storage system</returns>
    Task<string> UploadAsync(string objectKey, Stream content, string contentType);

    /// <summary>
    /// Uploads a file to the storage system and returns extended result
    /// </summary>
    /// <param name="objectKey">Object key or file name</param>
    /// <param name="content">File content stream</param>
    /// <param name="fileName">File name</param>
    /// <param name="documentId">Related document ID</param>
    /// <param name="contentType">File content type</param>
    /// <param name="bucketName">Target bucket name (uses default bucket if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload operation result</returns>
    Task<UploadResult> UploadWithResultAsync(string? objectKey, Stream content, string fileName, string documentId,
        string? contentType = null, string? bucketName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from the storage system
    /// </summary>
    /// <param name="objectKey">Key of the object to download</param>
    /// <returns>File content stream</returns>
    Task<Stream> DownloadAsync(string objectKey);

    /// <summary>
    /// Creates a URL for downloading a file from the storage system
    /// </summary>
    /// <param name="objectKey">Key of the object to download</param>
    /// <param name="expiryInSeconds">URL validity period in seconds</param>
    /// <returns>Download URL</returns>
    Task<string> GetDownloadUrlAsync(string objectKey, int expiryInSeconds = 3600);

    /// <summary>
    /// Deletes a file from the storage system
    /// </summary>
    /// <param name="objectKey">Key of the object to delete</param>
    Task DeleteAsync(string objectKey);

    /// <summary>
    /// Checks if an object exists in the storage system
    /// </summary>
    /// <param name="objectKey">Key of the object to check</param>
    /// <returns>True if object exists, false otherwise</returns>
    Task<bool> ExistsAsync(string objectKey);

    /// <summary>
    /// Calculates the hash value of a file
    /// </summary>
    /// <param name="content">Content stream</param>
    /// <returns>Hash value</returns>
    Task<string> GetHashAsync(Stream content);

    /// <summary>
    /// Unique identifier assigned to the storage provider
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Type of the storage provider
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Tests the connection status
    /// </summary>
    /// <returns>Test result</returns>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Lists all buckets/folders
    /// </summary>
    /// <returns>List of bucket information</returns>
    Task<IEnumerable<BucketInfoDto>> ListBucketsAsync();

    /// <summary>
    /// Checks if a bucket/folder exists
    /// </summary>
    /// <param name="bucketName">Bucket/folder name</param>
    /// <returns>True if bucket/folder exists, false otherwise</returns>
    Task<bool> BucketExistsAsync(string bucketName);

    /// <summary>
    /// Creates a new bucket/folder
    /// </summary>
    /// <param name="bucketName">Bucket/folder name</param>
    /// <returns>True if operation is successful, false otherwise</returns>
    Task<bool> CreateBucketAsync(string bucketName);

    /// <summary>
    /// Deletes a bucket/folder
    /// </summary>
    /// <param name="bucketName">Bucket/folder name</param>
    /// <param name="force">Whether to delete even if content exists</param>
    /// <returns>True if operation is successful, false otherwise</returns>
    Task<bool> RemoveBucketAsync(string bucketName, bool force = false);

    /// <summary>
    /// Checks if the provider supports the specified capability/feature
    /// </summary>
    /// <param name="capability">Capability/feature to check</param>
    /// <returns>True if capability is supported, false otherwise</returns>
    bool SupportsCapability(StorageCapability capability);

    /// <summary>
    /// Determines the provider-specific bucket search key from database bucket information
    /// </summary>
    /// <param name="bucket">Database bucket information</param>
    /// <returns>Key to use for searching bucket in provider</returns>
    string GetBucketSearchKey(StorageBucket bucket);
}