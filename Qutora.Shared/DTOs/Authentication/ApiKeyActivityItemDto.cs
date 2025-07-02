namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// API Key activity item
/// </summary>
public class ApiKeyActivityItemDto
{
    /// <summary>
    /// Activity ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Activity timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Request method (GET, POST, PUT, DELETE)
    /// </summary>
    public required string Method { get; set; }

    /// <summary>
    /// Request path
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Operation description
    /// </summary>
    public required string Description { get; set; }
}