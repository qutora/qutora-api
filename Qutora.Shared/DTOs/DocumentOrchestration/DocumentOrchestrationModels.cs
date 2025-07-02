using Microsoft.AspNetCore.Http;

namespace Qutora.Shared.DTOs.DocumentOrchestration;

/// <summary>
/// Document create request
/// </summary>
public class DocumentCreateRequest
{
    public IFormFile File { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? BucketId { get; set; }
    public string? MetadataJson { get; set; }
    public string? MetadataSchemaId { get; set; }
    public bool CreateShare { get; set; }
    public int? ExpiresAfterDays { get; set; }
    public int? MaxViewCount { get; set; }
    public string? Password { get; set; }
    public string? WatermarkText { get; set; }
    public bool AllowDownload { get; set; } = true;
    public bool AllowPrint { get; set; } = true;
    public string? CustomMessage { get; set; }
    public bool NotifyOnAccess { get; set; }
    public string? NotificationEmails { get; set; }
    public string UserId { get; set; } = string.Empty;
    /// <summary>
    /// Is this a direct share (direct file access)?
    /// </summary>
    public bool IsDirectShare { get; set; }
}

/// <summary>
/// Document update request
/// </summary>
public class DocumentUpdateRequest
{
    public Guid Id { get; set; }
    public UpdateDocumentDto UpdateDto { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Document delete request
/// </summary>
public class DocumentDeleteRequest
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Document download request
/// </summary>
public class DocumentDownloadRequest
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Document create result
/// </summary>
public class DocumentCreateResult
{
    public bool IsSuccess { get; set; }
    public DocumentDto? Document { get; set; }
    public DocumentShareDto? Share { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? ErrorDetails { get; set; }
    
    public DocumentCreateResponseDto? Response => Document != null ? 
        new DocumentCreateResponseDto { Document = Document, Share = Share } : null;
    
    public static DocumentCreateResult Success(DocumentDto document, DocumentShareDto? share = null) => 
        new() { IsSuccess = true, Document = document, Share = share };
    
    public static DocumentCreateResult Failure(string errorMessage, Dictionary<string, object>? details = null) => 
        new() { IsSuccess = false, ErrorMessage = errorMessage, ErrorDetails = details };
}

/// <summary>
/// Document update result
/// </summary>
public class DocumentUpdateResult
{
    public bool IsSuccess { get; set; }
    public DocumentDto? Document { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static DocumentUpdateResult Success(DocumentDto document) => 
        new() { IsSuccess = true, Document = document };
    
    public static DocumentUpdateResult Failure(string errorMessage) => 
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Document delete result
/// </summary>
public class DocumentDeleteResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static DocumentDeleteResult Success() => new() { IsSuccess = true };
    
    public static DocumentDeleteResult Failure(string errorMessage) => 
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Document download result
/// </summary>
public class DocumentDownloadResult
{
    public bool IsSuccess { get; set; }
    public Stream? FileStream { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static DocumentDownloadResult Success(Stream fileStream, string fileName, string contentType) => 
        new() { IsSuccess = true, FileStream = fileStream, FileName = fileName, ContentType = contentType };
    
    public static DocumentDownloadResult Failure(string errorMessage) => 
        new() { IsSuccess = false, ErrorMessage = errorMessage };
} 