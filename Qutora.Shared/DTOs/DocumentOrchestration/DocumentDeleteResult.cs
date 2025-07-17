namespace Qutora.Shared.DTOs.DocumentOrchestration;

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