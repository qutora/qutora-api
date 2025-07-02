namespace Qutora.Shared.DTOs.Common;

/// <summary>
/// DTO for pagination requests
/// </summary>
public class PageRequestDto
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Search term for filtering
    /// </summary>
    public string? SearchTerm { get; set; }
}