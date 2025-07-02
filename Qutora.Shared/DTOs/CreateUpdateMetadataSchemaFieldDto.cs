using System.ComponentModel.DataAnnotations;
using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for creating/updating metadata schema fields
/// </summary>
public class CreateUpdateMetadataSchemaFieldDto
{
    /// <summary>
    /// Field ID (for update scenarios)
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Field name
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Field display name
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Field description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Field type
    /// </summary>
    [Required]
    public MetadataType Type { get; set; }

    /// <summary>
    /// Is field required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Default value
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Minimum value (for numeric fields)
    /// </summary>
    public int? MinValue { get; set; }

    /// <summary>
    /// Maximum value (for numeric fields)
    /// </summary>
    public int? MaxValue { get; set; }

    /// <summary>
    /// Minimum length (for text fields)
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Maximum length (for text fields)
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Validation regex
    /// </summary>
    public string? ValidationRegex { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Option items (for Select/MultiSelect fields)
    /// </summary>
    public List<CreateUpdateMetadataSchemaFieldOptionDto>? OptionItems { get; set; } = new();
}