namespace Qutora.Shared.DTOs;

/// <summary>
/// Doküman içeriği ve meta verilerini taşıyan DTO
/// </summary>
public class DocumentContentDto
{
    /// <summary>
    /// Doküman içeriği (dosya akışı)
    /// </summary>
    public required byte[] Content { get; set; }

    /// <summary>
    /// Dosya adı
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Dosya içerik türü (MIME type)
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// Dosya boyutu (byte)
    /// </summary>
    public long FileSize { get; set; }
}