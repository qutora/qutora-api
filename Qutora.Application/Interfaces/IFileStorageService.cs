using Qutora.Domain.Entities;
using Qutora.Shared.DTOs.Common;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for file storage operations.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads file using the specified provider.
    /// </summary>
    /// <param name="providerName">Name of the storage provider to use</param>
    /// <param name="fileStream">File content</param>
    /// <param name="fileName">File name</param>
    /// <param name="contentType">File content type</param>
    /// <returns>Path of the uploaded file</returns>
    Task<string> UploadFileAsync(string providerName, Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// Uploads file using the specified provider.
    /// </summary>
    /// <param name="providerId">ID of the storage provider to use</param>
    /// <param name="fileStream">File content</param>
    /// <param name="fileName">File name</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="contentType">File content type</param>
    /// <param name="bucketName">Name of the bucket where the file will be uploaded</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result</returns>
    Task<UploadResult> UploadFileAsync(string providerId, Stream fileStream, string fileName, string documentId,
        string? contentType = null, string? bucketName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads file from the specified provider.
    /// </summary>
    /// <param name="providerName">Name of the storage provider to use</param>
    /// <param name="filePath">File path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of file content</returns>
    Task<Stream> DownloadFileAsync(string providerName, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads file from the specified provider.
    /// </summary>
    /// <param name="providerId">ID of the storage provider to use</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="fileName">File name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content and content type</returns>
    Task<(Stream FileStream, string ContentType)> DownloadFileAsync(string providerId, string documentId,
        string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes file from the specified provider.
    /// </summary>
    /// <param name="providerName">Name of the storage provider to use</param>
    /// <param name="filePath">File path</param>
    Task DeleteFileAsync(string providerName, string filePath);

    /// <summary>
    /// Deletes file from the specified provider.
    /// </summary>
    /// <param name="providerId">ID of the storage provider to use</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="fileName">File name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation is successful</returns>
    Task<bool> DeleteFileAsync(string providerId, string documentId, string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the file exists in the specified provider.
    /// </summary>
    /// <param name="providerName">Name of the storage provider to use</param>
    /// <param name="filePath">File path</param>
    /// <returns>True if file exists, false otherwise</returns>
    Task<bool> FileExistsAsync(string providerName, string filePath);

    /// <summary>
    /// Checks if the file exists in the specified provider.
    /// </summary>
    /// <param name="providerId">ID of the storage provider to use</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="fileName">File name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if file exists</returns>
    Task<bool> FileExistsAsync(string providerId, string documentId, string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the hash value of the file in the specified provider.
    /// </summary>
    public Task<string> GetFileHashAsync(Stream fileStream);


    /// <summary>
    /// Returns all available provider names (async version)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of provider names</returns>
    Task<IEnumerable<string>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the capabilities supported by the specified provider
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider capabilities</returns>
    Task<StorageProviderCapabilitiesDto> GetProviderCapabilitiesAsync(string providerId,
        CancellationToken cancellationToken = default);
}