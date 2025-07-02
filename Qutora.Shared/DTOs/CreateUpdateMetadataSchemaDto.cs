using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for creating and updating metadata schemas
/// </summary>
public class CreateUpdateMetadataSchemaDto
{
    /// <summary>
    /// Schema ID (for update scenarios)
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Schema name
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Schema description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Schema version
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Is schema active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Which file types this schema is valid for
    /// </summary>
    public string[]? FileTypes { get; set; }

    /// <summary>
    /// Which category this schema is valid for (required)
    /// </summary>
    [Required(ErrorMessage = "Category selection is required.")]
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Schema fields
    /// </summary>
    [Required]
    public List<CreateUpdateMetadataSchemaFieldDto> Fields { get; set; } = new();
}