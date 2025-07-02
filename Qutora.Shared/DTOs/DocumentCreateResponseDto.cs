namespace Qutora.Shared.DTOs;

/// <summary>
/// Response DTO for document creation operation
/// </summary>
public class DocumentCreateResponseDto
{
    /// <summary>
    /// Created document information
    /// </summary>
    public DocumentDto Document { get; set; } = null!;

    /// <summary>
    /// Created share information (if any)
    /// </summary>
    public DocumentShareDto? Share { get; set; }

    /// <summary>
    /// Was a share created?
    /// </summary>
    public bool HasShare => Share != null;

    /// <summary>
    /// Share URL (if any)
    /// </summary>
    public string? ShareUrl => Share != null ? $"/document/{Share.ShareCode}" : null;
}