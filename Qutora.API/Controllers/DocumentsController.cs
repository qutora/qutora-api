using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapsterMapper;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.DocumentOrchestration;
using Qutora.Shared.Enums;
using System.Security.Claims;
using Qutora.Application.Interfaces;

namespace Qutora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController(
    IDocumentOrchestrationService orchestrationService,
    IDocumentService documentService,
    IDocumentVersionService documentVersionService,
    IFileStorageService fileStorageService,
    ILogger<DocumentsController> logger,
    IMapper mapper,
    IMetadataService metadataService,
    IMetadataSchemaService metadataSchemaService,
    IStorageProviderService storageProviderService,
    IBucketPermissionManager permissionManager,
    IAuthorizationService authorizationService,
    IStorageBucketService bucketService,
    IDocumentAuthorizationService documentAuthorizationService)
    : ControllerBase
{
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    /// <summary>
    /// Gets a list of documents with optional filtering
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "Document.Read")]
    public async Task<ActionResult<PagedDto<DocumentDto>>> GetDocuments([FromQuery] string? query = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10, [FromQuery] Guid? providerId = null, [FromQuery] Guid? bucketId = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User identity not found");

            var isAdmin = await IsUserAdminAsync();
            var hasInactiveProviderAccess = await authorizationService.AuthorizeAsync(User, "Document.ViewInactiveProvider");

            PagedDto<DocumentDto> documents;

            if (providerId.HasValue)
                documents = await GetDocumentsByProviderAsync(providerId.Value, page, pageSize, query);
            else if (bucketId.HasValue)
                documents = await GetDocumentsByBucketAsync(bucketId.Value, userId, isAdmin, page, pageSize, query);
            else
                documents = await GetAllDocumentsAsync(userId, isAdmin, page, pageSize, query);

            if (!hasInactiveProviderAccess.Succeeded)
                documents = await FilterInactiveProviderDocumentsAsync(documents);

            return Ok(documents);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving documents");
            return StatusCode(500, "An error occurred while retrieving documents");
        }
    }

    private async Task<bool> IsUserAdminAsync()
    {
        var hasAdminAccess = await authorizationService.AuthorizeAsync(User, "Admin.Access");
        var hasDocumentAdmin = await authorizationService.AuthorizeAsync(User, "Document.Admin");
        return hasAdminAccess.Succeeded || hasDocumentAdmin.Succeeded;
    }

    private async Task<PagedDto<DocumentDto>> GetDocumentsByProviderAsync(Guid providerId, int page, int pageSize, string? query)
    {
        return await documentService.GetDocumentsByProviderAsync(providerId, page, pageSize, query);
    }

    private async Task<PagedDto<DocumentDto>> GetDocumentsByBucketAsync(Guid bucketId, string userId, bool isAdmin, int page, int pageSize, string? query)
    {
        if (!isAdmin)
        {
            var permissionCheck = await permissionManager.CheckUserBucketOperationPermissionAsync(
                userId, bucketId, PermissionLevel.Read);

            if (!permissionCheck.IsAllowed)
            {
                throw new UnauthorizedAccessException("Access denied to bucket");
            }
        }

        return await documentService.GetDocumentsByBucketAsync(bucketId, page, pageSize, query);
    }

    private async Task<PagedDto<DocumentDto>> GetAllDocumentsAsync(string userId, bool isAdmin, int page, int pageSize, string? query)
    {
        if (isAdmin)
        {
            return await documentService.GetDocumentsAsync(query, page, pageSize);
        }

        var allAccessibleBuckets = await bucketService.GetUserAccessiblePaginatedBucketsAsync(userId, 1, 1000);
        var bucketIds = allAccessibleBuckets.Items.Select(b => b.Id);
        
        return await documentService.GetDocumentsByBucketIdsAsync(bucketIds, page, pageSize, query);
    }

    private async Task<PagedDto<DocumentDto>> FilterInactiveProviderDocumentsAsync(PagedDto<DocumentDto> documents)
    {
        var activeProviders = await storageProviderService.GetAllActiveAsync();
        var activeProviderIds = activeProviders.Select(p => p.Id).ToHashSet();
        
        documents.Items = documents.Items.Where(d => activeProviderIds.Contains(d.StorageProviderId)).ToList();
        documents.TotalCount = documents.Items.Count;
        
        return documents;
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "Document.Read")]
    public async Task<IActionResult> GetDocument(Guid id, [FromQuery] bool includeMetadata = false)
    {
        try
        {
            var documentDto = await documentService.GetByIdAsync(id);
            if (documentDto == null) return NotFound();

            var providerValidation = await documentAuthorizationService.ValidateProviderAccessAsync(documentDto, User);
            if (!providerValidation.IsAuthorized)
            {
                return Forbid(providerValidation.Reason);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User identity not found");
            }

            if (includeMetadata)
            {
                var metadata = await metadataService.GetByDocumentIdAsync(id);
                documentDto.Metadata = metadata;
            }

            return Ok(documentDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving document with ID {DocumentId}", id);
            return StatusCode(500, "An error occurred while retrieving the document");
        }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = "Document.Create")]
    public async Task<IActionResult> CreateDocument(
        IFormFile file,
        [FromForm] string name,
        [FromForm] Guid? categoryId = null,
        [FromQuery] Guid? providerId = null,
        [FromQuery] Guid? bucketId = null,
        [FromForm] string? metadataJson = null,
        [FromForm] string? metadataSchemaId = null,
        [FromForm] bool createShare = false,
        [FromForm] int? expiresAfterDays = null,
        [FromForm] int? maxViewCount = null,
        [FromForm] string? password = null,
        [FromForm] string? watermarkText = null,
        [FromForm] bool allowDownload = true,
        [FromForm] bool allowPrint = true,
        [FromForm] string? customMessage = null,
        [FromForm] bool notifyOnAccess = false,
        [FromForm] string? notificationEmails = null,
        [FromForm] bool isDirectShare = false)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User identity not found");
            }

            var request = new DocumentCreateRequest
            {
                File = file,
                Name = name,
                CategoryId = categoryId,
                ProviderId = providerId,
                BucketId = bucketId,
                MetadataJson = metadataJson,
                MetadataSchemaId = metadataSchemaId,
                CreateShare = createShare,
                ExpiresAfterDays = expiresAfterDays,
                MaxViewCount = maxViewCount,
                Password = password,
                WatermarkText = watermarkText,
                AllowDownload = allowDownload,
                AllowPrint = allowPrint,
                CustomMessage = customMessage,
                NotifyOnAccess = notifyOnAccess,
                NotificationEmails = notificationEmails,
                UserId = userId,
                IsDirectShare = isDirectShare
            };

            var result = await orchestrationService.CreateDocumentAsync(request);

            if (!result.IsSuccess)
            {
                if (result.ErrorDetails != null)
                {
                    return BadRequest(new { 
                        error = result.ErrorMessage, 
                        details = result.ErrorDetails 
                    });
                }
                return BadRequest(result.ErrorMessage);
            }

            return CreatedAtAction(nameof(GetDocument), new { id = result.Document!.Id }, result.Response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating document");
            return StatusCode(500, "An error occurred while creating the document");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Document.Update")]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentDto updateDocumentDto)
    {
        try
        {
            if (updateDocumentDto == null) return BadRequest("Document data is required");

            if (id != updateDocumentDto.Id) return BadRequest("ID mismatch");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User identity not found");
            }

            var request = new DocumentUpdateRequest
            {
                Id = id,
                UpdateDto = updateDocumentDto,
                UserId = userId
            };

            var result = await orchestrationService.UpdateDocumentAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Document);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating document with ID {DocumentId}", id);
            return StatusCode(500, "An error occurred while updating the document");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Document.Delete")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User identity not found");
            }

            var request = new DocumentDeleteRequest
            {
                Id = id,
                UserId = userId
            };

            var result = await orchestrationService.DeleteDocumentAsync(request);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(result.ErrorMessage);
                }
                return BadRequest(result.ErrorMessage);
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting document with ID {DocumentId}", id);
            return StatusCode(500, "An error occurred while deleting the document");
        }
    }

    [HttpGet("{id}/download")]
    [Authorize(Policy = "Document.Read")]
    public async Task<IActionResult> DownloadDocument(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User identity not found");
            }

            var document = await documentService.GetByIdAsync(id);
            if (document == null)
            {
                return NotFound("Document not found");
            }

            var documentDto = _mapper.Map<DocumentDto>(document);
            
            // Check document access permission (includes bucket permission check)
            var accessValidation = await documentAuthorizationService.CanAccessDocumentAsync(userId, documentDto);
            if (!accessValidation.IsAuthorized)
            {
                return Forbid(accessValidation.Reason);
            }
            
            // Check provider access
            var providerValidation = await documentAuthorizationService.ValidateProviderAccessAsync(documentDto, User);
            if (!providerValidation.IsAuthorized)
            {
                return Forbid(providerValidation.Reason);
            }

            var request = new DocumentDownloadRequest
            {
                Id = id,
                UserId = userId
            };

            var result = await orchestrationService.DownloadDocumentAsync(request);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(result.ErrorMessage);
                }
                return BadRequest(result.ErrorMessage);
            }

            return File(result.FileStream!, result.ContentType!, result.FileName!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error downloading document with ID {DocumentId}", id);
            return StatusCode(500, "An error occurred while downloading the document");
        }
    }

    [HttpGet("category/{categoryId}")]
    [Authorize(Policy = "Document.Read")]
    public async Task<ActionResult<PagedDto<DocumentDto>>> GetDocumentsByCategory(Guid categoryId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var documentDtos = await documentService.GetByCategoryAsync(categoryId, page, pageSize);
            return Ok(documentDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving documents for category {CategoryId}", categoryId);
            return StatusCode(500, "An error occurred while retrieving documents");
        }
    }

    /// <summary>
    /// Gets available active storage providers
    /// </summary>
    [HttpGet("storage/providers")]
    [Authorize(Policy = "Document.Read")]
    public async Task<IActionResult> GetStorageProviders()
    {
        try
        {
            var providers = await storageProviderService.GetAllActiveAsync();
            return Ok(providers);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving storage providers");
            return StatusCode(500, "An error occurred while retrieving storage providers");
        }
    }

    /// <summary>
    /// Gets metadata schemas for document metadata
    /// </summary>
    [HttpGet("metadataSchemas")]
    [Authorize(Policy = "Document.Read")]
    public async Task<IActionResult> GetMetadataSchemas()
    {
        try
        {
            var schemas = await metadataSchemaService.GetAllAsync();
            var schemaDtos = _mapper.Map<List<MetadataSchemaDto>>(schemas);
            return Ok(schemaDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving metadata schemas");
            return StatusCode(500, "An error occurred while retrieving metadata schemas");
        }
    }

    #region Document Version Management

    /// <summary>
    /// Uploads a new file version for an existing document.
    /// </summary>
    /// <remarks>
    /// This endpoint creates a new file version for the specified document ID.
    /// If you want to create a new document, use the POST /api/documents endpoint.
    /// </remarks>
    /// <param name="id">Document ID</param>
    /// <param name="file">File to upload</param>
    /// <param name="changeDescription">Change description (optional)</param>
    /// <returns>Created version information</returns>
    [HttpPost("{id}/versions")]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = "Document.Update")]
    public async Task<ActionResult<DocumentVersionDto>> UploadNewVersion(Guid id, IFormFile file,
        [FromForm] string? changeDescription)
    {
        try
        {
            if (file.Length == 0) return BadRequest("File not uploaded or empty.");

            var providerValidation = await documentAuthorizationService.ValidateProviderAccessAsync(id, User);
            if (!providerValidation.IsAuthorized)
            {
                if (providerValidation.Reason?.Contains("not found") == true)
                {
                    return NotFound(providerValidation.Reason);
                }
                return Forbid(providerValidation.Reason);
            }

            await using var stream = file.OpenReadStream();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized("User ID could not be determined.");

            var result = await documentVersionService.CreateNewVersionAsync(
                id,
                stream,
                file.FileName,
                file.ContentType,
                userId,
                changeDescription);

            logger.LogInformation("New version created for document {DocumentId} (Version: {VersionNumber})",
                id, result.VersionNumber);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Argument error while creating version for document {DocumentId}", id);
            return NotFound(ex.Message);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "File error while creating version for document {DocumentId}", id);
            return StatusCode(500, $"File processing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while creating version for document {DocumentId}", id);
            return StatusCode(500, $"Version upload error: {ex.Message}");
        }
    }

    /// <summary>
    /// Lists all versions of a document.
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <returns>List of versions</returns>
    [HttpGet("{id}/versions")]
    [Authorize(Policy = "Document.Read")]
    public async Task<ActionResult<IEnumerable<DocumentVersionDto>>> GetVersions(Guid id)
    {
        try
        {
            var providerValidation = await documentAuthorizationService.ValidateProviderAccessAsync(id, User);
            if (!providerValidation.IsAuthorized)
            {
                if (providerValidation.Reason?.Contains("not found") == true)
                {
                    return NotFound(providerValidation.Reason);
                }
                return Forbid(providerValidation.Reason);
            }

            var versions = await documentVersionService.GetVersionsAsync(id);
            return Ok(versions);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Argument error while listing versions for document {DocumentId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while listing versions for document {DocumentId}", id);
            return StatusCode(500, $"Error listing versions: {ex.Message}");
        }
    }

    /// <summary>
    /// Downloads a specific document version.
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="versionId">Version ID</param>
    /// <returns>File contents</returns>
    [HttpGet("{documentId}/versions/{versionId}/download")]
    [Authorize(Policy = "Document.Read")]
    public async Task<ActionResult> DownloadVersion(Guid documentId, Guid versionId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User identity not found");
            }

            var versionDetails = await documentVersionService.GetVersionDetailAsync(versionId);
            if (versionDetails == null)
            {
                return NotFound("Version not found");
            }

            // Get document for access check
            var document = await documentService.GetByIdAsync(versionDetails.DocumentId);
            if (document == null)
            {
                return NotFound("Document not found");
            }

            var documentDto = _mapper.Map<DocumentDto>(document);
            
            // Check document access permission (includes bucket permission check)
            var accessValidation = await documentAuthorizationService.CanAccessDocumentAsync(userId, documentDto);
            if (!accessValidation.IsAuthorized)
            {
                return Forbid(accessValidation.Reason);
            }

            var providerValidation = await documentAuthorizationService.ValidateProviderAccessAsync(versionDetails.DocumentId, User);
            if (!providerValidation.IsAuthorized)
            {
                return Forbid(providerValidation.Reason);
            }

            var result = await documentVersionService.DownloadVersionAsync(versionId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Argument error while downloading version {VersionId}", versionId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while downloading version {VersionId}", versionId);
            return StatusCode(500, $"Version download error: {ex.Message}");
        }
    }

    /// <summary>
    /// Reverts a document to a specific version. Only admin and manager roles can use this.
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="versionId">Version ID</param>
    /// <returns>Updated version information</returns>
    [HttpPost("{documentId}/versions/{versionId}/rollback")]
    [Authorize(Policy = "Document.Admin")]
    public async Task<ActionResult<DocumentVersionDto>> RollbackToVersion(Guid documentId, Guid versionId)
    {
        try
        {
            var versionDetails = await documentVersionService.GetVersionDetailAsync(versionId);
            if (versionDetails == null)
            {
                return NotFound("Version not found");
            }

            var providerValidation = await documentAuthorizationService.ValidateProviderAccessAsync(versionDetails.DocumentId, User);
            if (!providerValidation.IsAuthorized)
            {
                return Forbid(providerValidation.Reason);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized("User ID could not be determined.");

            var result = await documentVersionService.RollbackToVersionAsync(versionId, userId);

            logger.LogInformation("Version {VersionId} restored (User: {UserId})", versionId, userId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Argument error while restoring version {VersionId}", versionId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while restoring version {VersionId}", versionId);
            return StatusCode(500, $"Version restore error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets detailed information about a specific version.
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="versionId">Version ID</param>
    /// <returns>Version details</returns>
    [HttpGet("{documentId}/versions/{versionId}/details")]
    [Authorize(Policy = "Document.Read")]
    public async Task<ActionResult<DocumentVersionDto>> GetVersionDetails(Guid documentId, Guid versionId)
    {
        try
        {
            var version = await documentVersionService.GetVersionDetailAsync(versionId);
            if (version == null)
            {
                return NotFound("Version not found");
            }
            var providerValidation = await documentAuthorizationService.ValidateProviderAccessAsync(version.DocumentId, User);
            if (!providerValidation.IsAuthorized)
            {
                return Forbid(providerValidation.Reason);
            }

            return Ok(version);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Argument error while getting version details {VersionId}", versionId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while getting version details {VersionId}", versionId);
            return StatusCode(500, $"Error retrieving version details: {ex.Message}");
        }
    }

    #endregion
}
