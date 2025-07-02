namespace Qutora.Shared.DTOs.DocumentOrchestration;

/// <summary>
/// Validation result container
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? ErrorDetails { get; set; }
    
    public static ValidationResult Success() => new() { IsValid = true };
    
    public static ValidationResult Failure(string errorMessage, Dictionary<string, object>? details = null) => 
        new() { IsValid = false, ErrorMessage = errorMessage, ErrorDetails = details };
} 