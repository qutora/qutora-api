namespace Qutora.Shared.DTOs;

/// <summary>
/// Standard response format used to return results of API operations
/// </summary>
/// <typeparam name="T">Type of data to be returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Data returned as result of the operation
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error message (if any)
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Creates an error result
    /// </summary>
    public static ApiResponse<T> ErrorResult(string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message
        };
    }
}