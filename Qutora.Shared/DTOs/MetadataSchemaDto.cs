namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO containing metadata schema information
/// </summary>
public class MetadataSchemaDto
{
    /// <summary>
    /// Schema ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Schema name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Schema description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Schema version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Is schema active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Schema fields
    /// </summary>
    public List<MetadataSchemaFieldDto> Fields { get; set; } = new();

    /// <summary>
    /// Supported file types
    /// </summary>
    public string[]? FileTypes { get; set; }

    /// <summary>
    /// Category ID that schema belongs to
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Category that schema belongs to (used by UI)
    /// </summary>
    public CategoryDto? Category { get; set; }

    /// <summary>
    /// Number of fields in schema (computed property for UI)
    /// </summary>
    public int FieldCount => Fields?.Count ?? 0;
}