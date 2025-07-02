namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO containing document metadata information
/// </summary>
public class MetadataDto
{
    /// <summary>
    /// Unique ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Related document ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Document name
    /// </summary>
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// Schema name
    /// </summary>
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Schema version
    /// </summary>
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// Metadata values
    /// </summary>
    public Dictionary<string, object> Values { get; set; } = new();

    /// <summary>
    /// Tags
    /// </summary>
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Modification date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}