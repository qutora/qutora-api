namespace Qutora.Application.Models.Validation;

/// <summary>
/// Metadata validation result
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Is validation result valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static ValidationResult Fail(List<ValidationError> errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }
}