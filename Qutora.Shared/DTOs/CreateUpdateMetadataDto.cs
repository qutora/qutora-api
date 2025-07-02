using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for creating/updating document metadata
/// </summary>
public class CreateUpdateMetadataDto
{
    /// <summary>
    /// Schema name (optional)
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// Metadata values (key-value pairs)
    /// </summary>
    [Required]
    public Dictionary<string, object> Values { get; set; } = new();

    /// <summary>
    /// Tags
    /// </summary>
    public string[]? Tags { get; set; }
}