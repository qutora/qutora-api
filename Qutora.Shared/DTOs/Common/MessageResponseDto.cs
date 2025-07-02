namespace Qutora.Shared.DTOs.Common;

/// <summary>
/// Message object for API responses
/// </summary>
public class MessageResponseDto
{
    /// <summary>
    /// Was the operation successful?
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message content
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Creates a successful response
    /// </summary>
    /// <param name="message">Message content</param>
    /// <returns>Message DTO</returns>
    public static MessageResponseDto SuccessResponse(string message)
    {
        return new MessageResponseDto
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    /// <param name="message">Error message</param>
    /// <returns>Message DTO</returns>
    public static MessageResponseDto ErrorResponse(string message)
    {
        return new MessageResponseDto
        {
            Success = false,
            Message = message
        };
    }
}