using Qutora.Domain.Entities;
using Microsoft.Extensions.Logging;
using Qutora.Infrastructure.Interfaces;
using Qutora.Infrastructure.Interfaces.Storage;
using Qutora.Shared.DTOs.Common;

namespace Qutora.Infrastructure.Storage.Providers;

/// <summary>
/// File storage service that supports multiple storage providers.
/// </summary>
public class FileStorageAdapter(IStorageManager providerManager, ILogger<FileStorageAdapter> logger)
    : IFileStorageService
{
    private readonly IStorageManager _providerManager =
        providerManager ?? throw new ArgumentNullException(nameof(providerManager));

    private readonly ILogger<FileStorageAdapter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var provider = await _providerManager.GetDefaultProviderAsync();
        var objectKey = $"{Guid.NewGuid()}-{fileName}";
        return await provider.UploadAsync(objectKey, fileStream, contentType);
    }

    /// <inheritdoc />
    public async Task<string> UploadFileAsync(string providerName, Stream fileStream, string fileName,
        string contentType)
    {
        var provider = await _providerManager.GetProviderAsync(providerName);

        var objectKey = $"{Guid.NewGuid()}-{fileName}";

        return await provider.UploadAsync(objectKey, fileStream, contentType);
    }

    /// <inheritdoc/>
    public async Task<UploadResult> UploadFileAsync(string providerId, Stream fileStream, string fileName,
        string documentId, string? contentType = null, string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _providerManager.GetProviderAsync(providerId);

            return await provider.UploadWithResultAsync(null, fileStream, fileName, documentId, contentType, bucketName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error uploading file {FileName} for document {DocumentId} using provider {ProviderId}", fileName,
                documentId, providerId);

            return new UploadResult
            {
                Success = false,
                FileName = fileName,
                FileId = documentId,
                ProviderName = providerId,
                ErrorMessage = ex.Message,
                UploadedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<Stream> DownloadFileAsync(string filePath)
    {
        var provider = await _providerManager.GetDefaultProviderAsync();
        return await provider.DownloadAsync(filePath);
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadFileAsync(string providerName, string filePath,
        CancellationToken cancellationToken = default)
    {
        var provider = await _providerManager.GetProviderAsync(providerName);
        return await provider.DownloadAsync(filePath);
    }

    /// <inheritdoc/>
    public async Task<(Stream FileStream, string ContentType)> DownloadFileAsync(string providerId, string documentId,
        string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _providerManager.GetProviderAsync(providerId);

            var objectKey = Path.Combine(documentId, fileName);

            var stream = await provider.DownloadAsync(objectKey);

            var contentType = DetermineContentType(fileName);

            return (stream, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error downloading file {FileName} for document {DocumentId} using provider {ProviderId}", fileName,
                documentId, providerId);
            throw;
        }
    }

    public async Task DeleteFileAsync(string filePath)
    {
        var provider = await _providerManager.GetDefaultProviderAsync();
        await provider.DeleteAsync(filePath);
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(string providerName, string filePath)
    {
        var provider = await _providerManager.GetProviderAsync(providerName);
        await provider.DeleteAsync(filePath);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteFileAsync(string providerId, string documentId, string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _providerManager.GetProviderAsync(providerId);

            var objectKey = Path.Combine(documentId, fileName);

            await provider.DeleteAsync(objectKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileName} for document {DocumentId} using provider {ProviderId}",
                fileName, documentId, providerId);
            return false;
        }
    }

    public async Task<string> GetFileHashAsync(Stream fileStream)
    {
        var provider = await _providerManager.GetDefaultProviderAsync();
        return await provider.GetHashAsync(fileStream);
    }


    public async Task<bool> FileExistsAsync(string filePath)
    {
        var provider = await _providerManager.GetDefaultProviderAsync();
        return await provider.ExistsAsync(filePath);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string providerName, string filePath)
    {
        var provider = await _providerManager.GetProviderAsync(providerName);
        return await provider.ExistsAsync(filePath);
    }

    /// <inheritdoc/>
    public async Task<bool> FileExistsAsync(string providerId, string documentId, string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _providerManager.GetProviderAsync(providerId);

            var objectKey = Path.Combine(documentId, fileName);

            return await provider.ExistsAsync(objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking if file {FileName} exists in document {DocumentId} using provider {ProviderId}",
                fileName, documentId, providerId);
            return false;
        }
    }


    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await _providerManager.GetAvailableProviderNamesAsync();
    }

    /// <summary>
    /// Determines content type based on file extension
    /// </summary>
    private string DetermineContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".csv" => "text/csv",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            _ => "application/octet-stream"
        };
    }


    /// <summary>
    /// Gets the default provider 
    /// </summary>
    public async Task<IStorageProvider> GetDefaultStorageProviderAsync()
    {
        return await _providerManager.GetDefaultProviderAsync();
    }

    /// <inheritdoc/>
    public async Task<StorageProviderCapabilitiesDto> GetProviderCapabilitiesAsync(string providerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _providerManager.GetProviderAsync(providerId);

            var capabilities = new StorageProviderCapabilitiesDto
            {
                ProviderId = providerId,
                SupportsCreateBucket = provider.SupportsCapability(Shared.Enums.StorageCapability.BucketCreation),
                SupportsDeleteBucket = provider.SupportsCapability(Shared.Enums.StorageCapability.BucketDeletion),
                SupportsListBuckets = provider.SupportsCapability(Shared.Enums.StorageCapability.BucketListing),
                SupportsForceDelete = provider.SupportsCapability(Shared.Enums.StorageCapability.ForceDelete),
                SupportsNestedBuckets = provider.SupportsCapability(Shared.Enums.StorageCapability.NestedBuckets),
                SupportsObjectMetadata = provider.SupportsCapability(Shared.Enums.StorageCapability.ObjectMetadata),
                SupportsObjectVersioning = provider.SupportsCapability(Shared.Enums.StorageCapability.ObjectVersioning)
            };

            return capabilities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining capabilities for provider {ProviderId}", providerId);

            return new StorageProviderCapabilitiesDto
            {
                ProviderId = providerId,
                SupportsCreateBucket = false,
                SupportsDeleteBucket = false,
                SupportsListBuckets = false,
                SupportsForceDelete = false,
                SupportsNestedBuckets = false,
                SupportsObjectMetadata = false,
                SupportsObjectVersioning = false
            };
        }
    }
}
