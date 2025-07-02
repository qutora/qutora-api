using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Interfaces;
using Qutora.Infrastructure.Interfaces.Storage;

namespace Qutora.API.Controllers;

/// <summary>
/// Direct file access API endpoint
/// Requires both bucket AND category AllowDirectAccess flags to be true
/// URL format: /api/direct-access/{documentId}.{extension}
/// </summary>
[ApiController]
[Route("api/direct-access")]
public class DirectAccessController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IStorageBucketService _bucketService;
    private readonly ICategoryService _categoryService;
    private readonly IStorageManager _storageManager;
    private readonly IDocumentShareService _documentShareService;
    private readonly ILogger<DirectAccessController> _logger;

    public DirectAccessController(
        IDocumentService documentService,
        IStorageBucketService bucketService,
        ICategoryService categoryService,
        IStorageManager storageManager,
        IDocumentShareService documentShareService,
        ILogger<DirectAccessController> logger)
    {
        _documentService = documentService;
        _bucketService = bucketService;
        _categoryService = categoryService;
        _storageManager = storageManager;
        _documentShareService = documentShareService;
        _logger = logger;
    }

    /// <summary>
    /// Get file directly by document ID and extension
    /// Both bucket and category must have AllowDirectAccess = true
    /// </summary>
    /// <param name="filename">Format: {documentId}.{extension}</param>
    /// <returns>File content with proper headers</returns>
    [HttpGet("{filename}")]
    public async Task<IActionResult> GetFile(string filename)
    {
        try
        {
            // Parse filename to extract document ID and extension
            var match = Regex.Match(filename, @"^([a-fA-F0-9\-]{36})\.([a-zA-Z0-9]+)$");
            if (!match.Success)
            {
                _logger.LogWarning("Invalid filename format: {Filename}", filename);
                return BadRequest("Invalid filename format. Expected: {guid}.{extension}");
            }

            var documentIdStr = match.Groups[1].Value;
            var extension = match.Groups[2].Value.ToLowerInvariant();

            if (!Guid.TryParse(documentIdStr, out var documentId))
            {
                _logger.LogWarning("Invalid document ID: {DocumentId}", documentIdStr);
                return BadRequest("Invalid document ID format");
            }

            // Get document
            var document = await _documentService.GetByIdAsync(documentId);
            if (document == null)
            {
                _logger.LogWarning("Document not found: {DocumentId}", documentId);
                return NotFound("Document not found");
            }

            // Check if document's file extension matches requested extension
            var documentExtension = Path.GetExtension(document.FileName)?.TrimStart('.').ToLowerInvariant();
            if (documentExtension != extension)
            {
                _logger.LogWarning("Extension mismatch for document {DocumentId}: requested {RequestedExt}, actual {ActualExt}", 
                    documentId, extension, documentExtension);
                return BadRequest("File extension does not match document");
            }

            // Check bucket permissions
            if (!document.BucketId.HasValue)
            {
                _logger.LogWarning("Document has no bucket assigned {DocumentId}", documentId);
                return BadRequest("Document has no bucket assigned");
            }

            var bucket = await _bucketService.GetBucketByIdAsync(document.BucketId.Value);
            if (bucket == null)
            {
                _logger.LogWarning("Bucket not found {BucketId} (document {DocumentId})", 
                    document.BucketId.Value, documentId);
                return NotFound("Bucket not found");
            }

            // Check category permissions
            if (!document.CategoryId.HasValue)
            {
                _logger.LogWarning("Document has no category assigned {DocumentId}", documentId);
                return BadRequest("Document has no category assigned");
            }

            var category = await _categoryService.GetByIdAsync(document.CategoryId.Value);
            if (category == null)
            {
                _logger.LogWarning("Category not found {CategoryId} (document {DocumentId})", 
                    document.CategoryId.Value, documentId);
                return NotFound("Category not found");
            }

            if (!bucket.AllowDirectAccess || !category.AllowDirectAccess)
            {
                _logger.LogWarning("Direct access denied - bucket or category does not allow direct access. Document {DocumentId}, Bucket: {BucketAccess}, Category: {CategoryAccess}", 
                    documentId, bucket.AllowDirectAccess, category.AllowDirectAccess);
                return StatusCode(403, "Direct access requires both bucket and category to allow direct access");
            }

            var directShares = await _documentShareService.GetByDocumentIdAsync(documentId);
            var approvedDirectShare = directShares?.FirstOrDefault(s => 
                s.IsDirectShare && s.ApprovalStatus == Qutora.Shared.Enums.ApprovalStatus.Approved);

            if (approvedDirectShare == null)
            {
                _logger.LogWarning("Direct access denied - no approved direct share found for document {DocumentId}", documentId);
                return StatusCode(403, "Direct access requires an approved direct share");
            }

            _logger.LogInformation("Direct access approved for document {DocumentId} via direct share {ShareId}", 
                documentId, approvedDirectShare.Id);

            _logger.LogInformation("Direct access security checks passed for document {DocumentId}", documentId);

            // Get storage provider and download file
            var storageProvider = await _storageManager.GetProviderAsync(bucket.ProviderId.ToString());
            using var fileStream = await storageProvider.DownloadAsync(document.StoragePath);
            
            if (fileStream == null || !fileStream.CanRead)
            {
                _logger.LogWarning("File content not found for document {DocumentId}", documentId);
                return NotFound("File content not found");
            }

            // Read file content into memory
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var fileContent = memoryStream.ToArray();
            
            if (fileContent.Length == 0)
            {
                _logger.LogWarning("File content is empty for document {DocumentId}", documentId);
                return NotFound("File content is empty");
            }

            // Determine content type
            var contentType = GetContentType(extension);

            // Set response headers
            Response.Headers["Cache-Control"] = "public, max-age=3600"; // 1 hour cache
            Response.Headers["ETag"] = $"\"{document.Id}_{document.UpdatedAt:yyyyMMddHHmmss}\"";

            _logger.LogInformation("Direct access successful for document {DocumentId}.{Extension}, size: {Size} bytes", 
                documentId, extension, fileContent.Length);

            return File(fileContent, contentType, document.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in direct file access for: {Filename}", filename);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get content type based on file extension
    /// </summary>
    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            "webp" => "image/webp",
            "svg" => "image/svg+xml",
            "pdf" => "application/pdf",
            "txt" => "text/plain",
            "html" or "htm" => "text/html",
            "css" => "text/css",
            "js" => "application/javascript",
            "json" => "application/json",
            "xml" => "application/xml",
            "mp4" => "video/mp4",
            "mp3" => "audio/mpeg",
            "wav" => "audio/wav",
            "doc" => "application/msword",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "xls" => "application/vnd.ms-excel",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "ppt" => "application/vnd.ms-powerpoint",
            "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
    }
} 