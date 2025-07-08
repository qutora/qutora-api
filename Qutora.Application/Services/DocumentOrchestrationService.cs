using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Interfaces;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.DocumentOrchestration;

namespace Qutora.Application.Services;

/// <summary>
/// Document orchestration service implementation
/// </summary>
public class DocumentOrchestrationService(
    IDocumentService documentService,
    IDocumentValidationService validationService,
    IDocumentAuthorizationService authorizationService,
    IDocumentStorageService storageService,
    IMetadataSchemaService metadataSchemaService,
    IAuditService auditService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<DocumentOrchestrationService> logger)
    : IDocumentOrchestrationService
{
    public async Task<DocumentCreateResult> CreateDocumentAsync(DocumentCreateRequest request)
    {
        try
        {
            // Step 1: File validation
            var fileValidation = await validationService.ValidateFileAsync(
                request.File, 
                request.ProviderId, 
                request.MetadataSchemaId);

            if (!fileValidation.IsValid)
            {
                return DocumentCreateResult.Failure(fileValidation.ErrorMessage!, fileValidation.ErrorDetails);
            }

            // Step 2: Storage selection
            var storageSelection = await storageService.SelectOptimalStorageAsync(
                request.UserId, 
                request.ProviderId, 
                request.BucketId);

            if (!storageSelection.IsSuccess)
            {
                return DocumentCreateResult.Failure(storageSelection.ErrorMessage!);
            }

            // Update request with selected storage
            request.ProviderId = storageSelection.ProviderId;
            request.BucketId = storageSelection.BucketId;

            // Step 3: Authorization check
            var authorizationCheck = await authorizationService.CanCreateDocumentAsync(
                request.UserId, 
                request.ProviderId, 
                request.BucketId);

            if (!authorizationCheck.IsAuthorized)
            {
                return DocumentCreateResult.Failure(authorizationCheck.Reason!);
            }

            // Step 4: Metadata validation
            string? schemaName = null;
            if (!string.IsNullOrEmpty(request.MetadataSchemaId) && Guid.TryParse(request.MetadataSchemaId, out var schemaId))
            {
                var schema = await metadataSchemaService.GetByIdAsync(schemaId);
                if (schema != null)
                {
                    schemaName = schema.Name;
                }
            }

            var metadataValidation = await validationService.ValidateMetadataAsync(request.MetadataJson, schemaName);
            if (!metadataValidation.IsValid)
            {
                return DocumentCreateResult.Failure(metadataValidation.ErrorMessage!);
            }

            // Step 5: Prepare share options
            DocumentShareCreateDto? shareOptions = null;
            if (request.CreateShare)
            {
                var emailList = new List<string>();
                if (!string.IsNullOrEmpty(request.NotificationEmails))
                {
                    emailList.AddRange(request.NotificationEmails.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrEmpty(e)));
                }

                shareOptions = new DocumentShareCreateDto
                {
                    ExpiresAfterDays = request.ExpiresAfterDays,
                    MaxViewCount = request.MaxViewCount,
                    Password = request.Password,
                    WatermarkText = request.WatermarkText,
                    AllowDownload = request.AllowDownload,
                    AllowPrint = request.AllowPrint,
                    CustomMessage = request.CustomMessage,
                    NotifyOnAccess = request.NotifyOnAccess,
                    NotificationEmails = emailList,
                    IsDirectShare = request.IsDirectShare
                };
            }

            // Step 6: Create document
            var (documentDto, shareDto) = await documentService.CreateDocumentWithFileAsync(
                request.File,
                request.Name,
                request.CategoryId,
                request.ProviderId,
                request.BucketId,
                request.MetadataJson,
                schemaName,
                shareOptions);

            // 🔥 SERVICE KATMANINDA AUDIT LOG
            _ = Task.Run(async () =>
            {
                try
                {
                    await LogDocumentCreatedAsync(request.UserId, documentDto.Id, documentDto.Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to log document creation audit for DocumentId: {DocumentId}", documentDto.Id);
                }
            });

            return DocumentCreateResult.Success(documentDto, shareDto);
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid argument in document creation");
            return DocumentCreateResult.Failure(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogError(ex, "Resource not found in document creation");
            return DocumentCreateResult.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Invalid operation in document creation");
            return DocumentCreateResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in document creation");
            return DocumentCreateResult.Failure("An error occurred while creating the document");
        }
    }

    public async Task<DocumentUpdateResult> UpdateDocumentAsync(DocumentUpdateRequest request)
    {
        try
        {
            // Step 1: Get existing document
            var existingDocument = await documentService.GetByIdAsync(request.Id);
            if (existingDocument == null)
            {
                return DocumentUpdateResult.Failure("Document not found");
            }

            // Step 2: Authorization check
            var authorizationCheck = await authorizationService.CanUpdateDocumentAsync(
                request.UserId, 
                existingDocument, 
                request.UpdateDto);

            if (!authorizationCheck.IsAuthorized)
            {
                return DocumentUpdateResult.Failure(authorizationCheck.Reason!);
            }

            // Step 3: Update document
            var updatedDocument = await documentService.UpdateAsync(request.Id, request.UpdateDto);

            // 🔥 SERVICE KATMANINDA AUDIT LOG
            _ = Task.Run(async () =>
            {
                try
                {
                    await LogDocumentUpdatedAsync(request.UserId, request.Id, updatedDocument.Name, request.UpdateDto);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to log document update audit for DocumentId: {DocumentId}", request.Id);
                }
            });

            return DocumentUpdateResult.Success(updatedDocument);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating document {DocumentId}", request.Id);
            return DocumentUpdateResult.Failure("An error occurred while updating the document");
        }
    }

    public async Task<DocumentDeleteResult> DeleteDocumentAsync(DocumentDeleteRequest request)
    {
        try
        {
            // Step 1: Get existing document
            var existingDocument = await documentService.GetByIdAsync(request.Id);
            if (existingDocument == null)
            {
                return DocumentDeleteResult.Failure("Document not found");
            }

            // Step 2: Authorization check
            var authorizationCheck = await authorizationService.CanDeleteDocumentAsync(request.UserId, existingDocument);

            if (!authorizationCheck.IsAuthorized)
            {
                return DocumentDeleteResult.Failure(authorizationCheck.Reason!);
            }

            // Step 3: Delete document
            var result = await documentService.DeleteAsync(request.Id);

            if (!result)
            {
                return DocumentDeleteResult.Failure("Document could not be deleted");
            }

            // 🔥 SERVICE KATMANINDA AUDIT LOG
            _ = Task.Run(async () =>
            {
                try
                {
                    await LogDocumentDeletedAsync(request.UserId, request.Id, existingDocument.Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to log document deletion audit for DocumentId: {DocumentId}", request.Id);
                }
            });

            return DocumentDeleteResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting document {DocumentId}", request.Id);
            return DocumentDeleteResult.Failure("An error occurred while deleting the document");
        }
    }

    public async Task<DocumentDownloadResult> DownloadDocumentAsync(DocumentDownloadRequest request)
    {
        try
        {
            // Step 1: Get document
            var document = await documentService.GetByIdAsync(request.Id);
            if (document == null)
            {
                return DocumentDownloadResult.Failure("Document not found");
            }

            // Step 2: Authorization check
            var authorizationCheck = await authorizationService.CanAccessDocumentAsync(request.UserId, document);

            if (!authorizationCheck.IsAuthorized)
            {
                return DocumentDownloadResult.Failure(authorizationCheck.Reason!);
            }

            // Step 3: Download document
            var fileStream = await documentService.DownloadAsync(request.Id);

            var result = DocumentDownloadResult.Success(fileStream, document.FileName, document.ContentType);

            // 🔥 SERVICE KATMANINDA AUDIT LOG
            _ = Task.Run(async () =>
            {
                try
                {
                    await LogDocumentDownloadAsync(request.UserId, request.Id, document.FileName, document.FileSize, "DirectDownload");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to log document download audit for DocumentId: {DocumentId}", request.Id);
                }
            });

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error downloading document {DocumentId}", request.Id);
            return DocumentDownloadResult.Failure("An error occurred while downloading the document");
        }
    }

    // 🔥 PRIVATE AUDIT LOG METHODS
    private async Task LogDocumentCreatedAsync(string userId, Guid documentId, string documentName)
    {
        var httpContextData = GetHttpContextData();
        await auditService.LogDocumentCreatedAsync(userId, documentId, documentName, httpContextData);
    }

    private async Task LogDocumentUpdatedAsync(string userId, Guid documentId, string documentName, UpdateDocumentDto updateDto)
    {
        var httpContextData = GetHttpContextData();
        var changes = new Dictionary<string, object>
        {
            {"Name", updateDto.Name},
            {"CategoryId", updateDto.CategoryId},
            {"BucketId", updateDto.BucketId}
        };

        await auditService.LogDocumentUpdatedAsync(userId, documentId, documentName, changes, httpContextData);
    }

    private async Task LogDocumentDeletedAsync(string userId, Guid documentId, string documentName)
    {
        var httpContextData = GetHttpContextData();
        await auditService.LogDocumentDeletedAsync(userId, documentId, documentName, httpContextData);
    }

    private async Task LogDocumentDownloadAsync(string userId, Guid documentId, string fileName, long fileSize, string downloadType)
    {
        var httpContextData = GetHttpContextData();
        await auditService.LogDocumentDownloadedAsync(userId, documentId, fileName, fileSize, downloadType, httpContextData);
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
} 