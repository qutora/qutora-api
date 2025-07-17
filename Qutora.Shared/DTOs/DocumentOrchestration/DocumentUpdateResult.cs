namespace Qutora.Shared.DTOs.DocumentOrchestration;

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