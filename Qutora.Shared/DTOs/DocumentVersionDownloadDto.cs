namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for document version download information
/// </summary>
public class DocumentVersionDownloadDto
{
    /// <summary>
    /// Download URL
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// File MIME type
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// URL validity period (seconds)
    /// </summary>
    public int ExpiresInSeconds { get; set; }

    /// <summary>
    /// URL generation time
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}