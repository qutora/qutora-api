using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Qutora.Application.Interfaces;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Exceptions;
using Qutora.Infrastructure.Interfaces;
using Qutora.Infrastructure.Interfaces.Storage;
using Qutora.Infrastructure.Interfaces.UnitOfWork;
using Qutora.Shared.DTOs;
using System.Security.Claims;

namespace Qutora.Application.Services;

/// <summary>
/// Service implementation for document versioning operations
/// </summary>
public class DocumentVersionService(
    IUnitOfWork unitOfWork,
    IStorageManager storageManager,
    IAuditService auditService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<DocumentVersionService> logger)
    : IDocumentVersionService
{
    /// <inheritdoc/>
    public async Task<DocumentVersionDto> CreateNewVersionAsync(
        Guid documentId,
        Stream fileStream,
        string fileName,
        string contentType,
        string userId,
        string? changeDescription = null)
    {
        try
        {
            return await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                var document = await unitOfWork.Documents.GetByIdAsync(documentId);
                if (document == null)
                    throw new ArgumentException($"Document not found. ID: {documentId}", nameof(documentId));

                var lastVersionNumber = await unitOfWork.DocumentVersions.GetLastVersionNumberAsync(documentId);
                var newVersionNumber = lastVersionNumber + 1;

                var storageProvider = await storageManager.GetProviderAsync(document.StorageProviderId.ToString());

                string? bucketName = null;
                if (document.BucketId.HasValue)
                {
                    var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(document.BucketId.Value);
                    bucketName = bucket?.Path;
                    logger.LogInformation(
                        "Document has bucket assigned. BucketId: {BucketId}, BucketName: {BucketName}",
                        document.BucketId.Value, bucketName);
                }

                // Generate unique path to prevent collision - add timestamp and GUID
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                var uniqueId = Guid.NewGuid().ToString("N")[..8]; 
                var safeFileName = Path.GetFileName(fileName);
                var versionPath = $"versions/{documentId}/{newVersionNumber}/{timestamp}-{uniqueId}-{safeFileName}";
                
                var uploadResult = await storageProvider.UploadWithResultAsync(
                    versionPath,
                    fileStream,
                    fileName,
                    documentId.ToString(),
                    contentType,
                    bucketName);

                if (!uploadResult.Success) throw new IOException($"File upload error: {uploadResult.ErrorMessage}");

                var version = new DocumentVersion
                {
                    DocumentId = documentId,
                    VersionNumber = newVersionNumber,
                    FileName = uploadResult.FileName,
                    StoragePath = uploadResult.StoragePath,
                    FileSize = uploadResult.FileSize,
                    MimeType = uploadResult.ContentType,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    ChangeDescription = changeDescription,
                    Hash = uploadResult.FileHash
                };

                await unitOfWork.DocumentVersions.AddAsync(version);

                await unitOfWork.SaveChangesAsync();

                document.CurrentVersionId = version.Id;
                document.FileName = version.FileName;
                document.ContentType = version.MimeType;
                document.FileSize = version.FileSize;
                document.StoragePath = version.StoragePath;
                document.Hash = version.Hash ?? document.Hash;

                await unitOfWork.Documents.UpdateAsync(document);

                await auditService.LogDocumentVersionCreatedAsync(
                    userId,
                    documentId,
                    version.Id,
                    newVersionNumber,
                    changeDescription != null
                        ? new Dictionary<string, string> { { "changeDescription", changeDescription } }
                        : null);

                return new DocumentVersionDto
                {
                    Id = version.Id,
                    DocumentId = document.Id,
                    DocumentName = document.Name,
                    VersionNumber = version.VersionNumber,
                    FileName = version.FileName,
                    FileSize = version.FileSize,
                    MimeType = version.MimeType,
                    StoragePath = version.StoragePath,
                    CreatedAt = version.CreatedAt,
                    CreatedBy = version.CreatedBy,
                    CreatedByName = version.CreatedByUser?.UserName ?? "Unknown User",
                    ChangeDescription = version.ChangeDescription,
                    IsCurrent = document.CurrentVersionId == version.Id
                };
            });
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency error occurred while creating version. Document ID: {DocumentId}",
                documentId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating version. Document ID: {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentVersionDto>> GetVersionsAsync(Guid documentId)
    {
        try
        {
            var document = await unitOfWork.Documents.GetByIdAsync(documentId);
            if (document == null)
                throw new ArgumentException($"Document not found. ID: {documentId}", nameof(documentId));

            var versions = await unitOfWork.DocumentVersions.GetByDocumentIdAsync(documentId);

            return versions.Select(v => new DocumentVersionDto
            {
                Id = v.Id,
                DocumentId = document.Id,
                DocumentName = document.Name,
                VersionNumber = v.VersionNumber,
                FileName = v.FileName,
                FileSize = v.FileSize,
                MimeType = v.MimeType,
                CreatedAt = v.CreatedAt,
                CreatedBy = v.CreatedBy,
                CreatedByName = v.CreatedByUser?.UserName ?? "Unknown User",
                ChangeDescription = v.ChangeDescription,
                IsCurrent = document.CurrentVersionId == v.Id
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while listing versions. Document ID: {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentVersionDownloadDto> DownloadVersionAsync(Guid versionId)
    {
        try
        {
            var version = await unitOfWork.DocumentVersions.GetDetailAsync(versionId);
            if (version == null)
                throw new ArgumentException($"Version not found. ID: {versionId}", nameof(versionId));

            var document = await unitOfWork.Documents.GetByIdAsync(version.DocumentId);
            if (document == null) throw new ArgumentException($"Document not found. ID: {version.DocumentId}");

            var storageProvider = await storageManager.GetProviderAsync(document.StorageProviderId.ToString());

            var expiryInSeconds = 3600;

            var downloadUrl = await storageProvider.GetDownloadUrlAsync(version.StoragePath, expiryInSeconds);

            return await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                document.LastAccessedAt = DateTime.UtcNow;
                await unitOfWork.Documents.UpdateAsync(document);

                var result = new DocumentVersionDownloadDto
                {
                    DownloadUrl = downloadUrl,
                    FileName = version.FileName,
                    FileSize = version.FileSize,
                    MimeType = version.MimeType,
                    GeneratedAt = DateTime.UtcNow,
                    ExpiresInSeconds = expiryInSeconds
                };

                // 🔥 SERVICE KATMANINDA AUDIT LOG
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await LogVersionDownloadAsync(
                            version.DocumentId,
                            versionId,
                            version.VersionNumber,
                            version.FileName,
                            version.FileSize);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to log version download audit for VersionId: {VersionId}", versionId);
                    }
                });

                return result;
            });
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency error occurred while downloading version: {VersionId}", versionId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while downloading version. Version ID: {VersionId}", versionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentVersionDto> RollbackToVersionAsync(Guid versionId, string userId)
    {
        try
        {
            return await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                var version = await unitOfWork.DocumentVersions.GetDetailAsync(versionId);
                if (version == null)
                    throw new ArgumentException($"Version not found. ID: {versionId}", nameof(versionId));

                var document = await unitOfWork.Documents.GetByIdAsync(version.DocumentId);
                if (document == null) throw new ArgumentException($"Document not found. ID: {version.DocumentId}");

                if (document.CurrentVersionId == version.Id)
                {
                    logger.LogInformation("Version is already current, no rollback performed: {VersionId}",
                        versionId);

                    return new DocumentVersionDto
                    {
                        Id = version.Id,
                        DocumentId = document.Id,
                        DocumentName = document.Name,
                        VersionNumber = version.VersionNumber,
                        FileName = version.FileName,
                        FileSize = version.FileSize,
                        MimeType = version.MimeType,
                        StoragePath = version.StoragePath,
                        CreatedAt = version.CreatedAt,
                        CreatedBy = version.CreatedBy,
                        CreatedByName = version.CreatedByUser?.UserName ?? "Unknown User",
                        ChangeDescription = version.ChangeDescription,
                        IsCurrent = true
                    };
                }

                document.CurrentVersionId = version.Id;
                document.FileName = version.FileName;
                document.ContentType = version.MimeType;
                document.FileSize = version.FileSize;
                document.StoragePath = version.StoragePath;
                document.LastAccessedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(version.Hash)) document.Hash = version.Hash;

                await unitOfWork.Documents.UpdateAsync(document);

                await auditService.LogDocumentVersionRolledBackAsync(
                    userId,
                    version.DocumentId,
                    version.Id,
                    version.VersionNumber,
                    new Dictionary<string, string>
                    {
                        { "rollbackTimestamp", DateTime.UtcNow.ToString("o") }
                    });

                return new DocumentVersionDto
                {
                    Id = version.Id,
                    DocumentId = document.Id,
                    DocumentName = document.Name,
                    VersionNumber = version.VersionNumber,
                    FileName = version.FileName,
                    FileSize = version.FileSize,
                    MimeType = version.MimeType,
                    StoragePath = version.StoragePath,
                    CreatedAt = version.CreatedAt,
                    CreatedBy = version.CreatedBy,
                    CreatedByName = version.CreatedByUser?.UserName ?? "Unknown User",
                    ChangeDescription = version.ChangeDescription,
                    IsCurrent = true
                };
            });
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency error occurred while rolling back to version: {VersionId}",
                versionId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while rolling back to version. Version ID: {VersionId}", versionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentVersionDto> GetVersionDetailAsync(Guid versionId)
    {
        try
        {
            var version = await unitOfWork.DocumentVersions.GetDetailAsync(versionId);
            if (version == null)
                throw new ArgumentException($"Version not found. ID: {versionId}", nameof(versionId));

            var document = await unitOfWork.Documents.GetByIdAsync(version.DocumentId);
            if (document == null) throw new ArgumentException($"Document not found. ID: {version.DocumentId}");

            return new DocumentVersionDto
            {
                Id = version.Id,
                DocumentId = document.Id,
                DocumentName = document.Name,
                VersionNumber = version.VersionNumber,
                FileName = version.FileName,
                FileSize = version.FileSize,
                MimeType = version.MimeType,
                StoragePath = version.StoragePath,
                CreatedAt = version.CreatedAt,
                CreatedBy = version.CreatedBy,
                CreatedByName = version.CreatedByUser?.UserName ?? "Unknown User",
                ChangeDescription = version.ChangeDescription,
                IsCurrent = document.CurrentVersionId == version.Id
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while retrieving version details. Version ID: {VersionId}", versionId);
            throw;
        }
    }

    private async Task LogVersionDownloadAsync(Guid documentId, Guid versionId, int versionNumber, string fileName, long fileSize)
    {
        var httpContextData = GetHttpContextData();
        var userId = GetCurrentUserId();

        await auditService.LogDocumentVersionDownloadedAsync(
            userId,
            documentId,
            versionId,
            versionNumber,
            fileName,
            fileSize,
            httpContextData);
    }

    /// <summary>
    /// Get HTTP context information for audit logging
    /// </summary>
    private Dictionary<string, string> GetHttpContextData()
    {
        var httpContext = httpContextAccessor.HttpContext;
        return new Dictionary<string, string>
        {
            {"ipAddress", httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown"},
            {"userAgent", httpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown"}
        };
    }

    /// <summary>
    /// Get current user ID from HTTP context
    /// </summary>
    private string GetCurrentUserId()
    {
        var httpContext = httpContextAccessor.HttpContext;
        return httpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "unknown";
    }
}
