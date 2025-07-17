namespace Qutora.Shared.DTOs.DocumentOrchestration;

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