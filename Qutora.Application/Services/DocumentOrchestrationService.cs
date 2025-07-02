using Microsoft.Extensions.Logging;
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

            return DocumentDownloadResult.Success(fileStream, document.FileName, document.ContentType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error downloading document {DocumentId}", request.Id);
            return DocumentDownloadResult.Failure("An error occurred while downloading the document");
        }
    }
} 