using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for creating/updating metadata schema field options
/// </summary>
public class CreateUpdateMetadataSchemaFieldOptionDto
{
    /// <summary>
    /// Display label
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Actual value (if empty, Label is used)
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Is default value
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Option order
    /// </summary>
    public int Order { get; set; } = 0;
} 