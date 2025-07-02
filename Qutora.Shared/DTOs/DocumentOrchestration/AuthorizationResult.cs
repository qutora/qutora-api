namespace Qutora.Shared.DTOs.DocumentOrchestration;

/// <summary>
/// Document authorization result container
/// </summary>
public class DocumentAuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public string? Reason { get; set; }
    
    public static DocumentAuthorizationResult Success() => new() { IsAuthorized = true };
    
    public static DocumentAuthorizationResult Failure(string reason) => 
        new() { IsAuthorized = false, Reason = reason };
} 