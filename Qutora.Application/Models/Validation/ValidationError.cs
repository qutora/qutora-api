namespace Qutora.Application.Models.Validation;

/// <summary>
/// Metadata validation error
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Field with error
    /// </summary>
    public string Field { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Creates a new validation error
    /// </summary>
    public ValidationError(string field, string message)
    {
        Field = field;
        Message = message;
    }
}