namespace Qutora.Shared.DTOs.Common;

/// <summary>
/// Standard error response format
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public string Details { get; set; }

    /// <summary>
    /// Creates a new error response with the specified code and message
    /// </summary>
    public static ErrorResponse Create(string code, string message)
    {
        return new ErrorResponse { Code = code, Message = message };
    }

    /// <summary>
    /// Creates a new error response with the specified code, message and details
    /// </summary>
    public static ErrorResponse Create(string code, string message, string details)
    {
        return new ErrorResponse { Code = code, Message = message, Details = details };
    }
}