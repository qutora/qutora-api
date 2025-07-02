using Microsoft.AspNetCore.Http;
using Qutora.Shared.DTOs;

namespace Qutora.Infrastructure.Storage;

/// <summary>
/// Storage provider adapter interface
/// </summary>
public interface IStorageProviderAdapter
{
    string ProviderId { get; }

    /// <summary>
    /// Returns the provider type (FileSystem, S3, FTP, etc.)
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Uploads a file
    /// </summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string? contentType = null,
        string? bucketName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file
    /// </summary>
    Task<string> UploadFileAsync(IFormFile file, string? fileName = null, string? bucketName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file
    /// </summary>
    Task<Stream> DownloadFileAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file
    /// </summary>
    Task<bool> DeleteFileAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the file exists
    /// </summary>
    Task<bool> FileExistsAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files
    /// </summary>
    Task<IEnumerable<string>> ListFilesAsync(string? prefix = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a temporary URL for the file
    /// </summary>
    Task<string> GetTemporaryUrlAsync(string fileName, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>
    /// For connection testing
    /// </summary>
    Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all buckets/folders
    /// </summary>
    /// <returns>List of bucket information</returns>
    Task<IEnumerable<BucketInfoDto>> ListBucketsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a bucket/folder exists
    /// </summary>
    /// <param name="bucketName">Bucket/folder name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if bucket/folder exists, false otherwise</returns>
    Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new bucket/folder
    /// </summary>
    /// <param name="bucketName">Bucket/folder name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation is successful, false otherwise</returns>
    Task<bool> CreateBucketAsync(string bucketName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a bucket/folder
    /// </summary>
    /// <param name="bucketName">Bucket/folder name</param>
    /// <param name="force">Whether to perform deletion even if content exists</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation is successful, false otherwise</returns>
    Task<bool> RemoveBucketAsync(string bucketName, bool force = false, CancellationToken cancellationToken = default);
}
