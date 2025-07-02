namespace Qutora.Shared.DTOs;

/// <summary>
/// Data transfer object for document metadata information
/// </summary>
public class DocumentMetadataDto
{
    /// <summary>
    /// Metadata schema name
    /// </summary>
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Schema ID
    /// </summary>
    public Guid? SchemaId { get; set; }

    /// <summary>
    /// Metadata fields
    /// </summary>
    public Dictionary<string, string> Values { get; set; } = new();
}