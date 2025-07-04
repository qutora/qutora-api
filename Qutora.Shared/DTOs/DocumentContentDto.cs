namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for transferring document content and metadata
/// </summary>
public class DocumentContentDto
{
    /// <summary>
    /// Document content (file stream)
    /// </summary>
    public required byte[] Content { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// File content type (MIME type)
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// File size (bytes)
    /// </summary>
    public long FileSize { get; set; }
}