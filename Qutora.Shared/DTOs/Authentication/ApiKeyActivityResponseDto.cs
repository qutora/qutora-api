namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// API Key activity response model
/// </summary>
public class ApiKeyActivityResponseDto
{
    /// <summary>
    /// API Key ID
    /// </summary>
    public Guid ApiKeyId { get; set; }

    /// <summary>
    /// API Key name
    /// </summary>
    public required string ApiKeyName { get; set; }

    /// <summary>
    /// Total activity count
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Activity list
    /// </summary>
    public required List<ApiKeyActivityItemDto> Activities { get; set; }
} 