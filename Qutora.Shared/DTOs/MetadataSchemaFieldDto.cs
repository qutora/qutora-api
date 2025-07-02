using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs;

/// <summary>
/// Data transfer object for metadata schema fields
/// </summary>
public class MetadataSchemaFieldDto
{
    /// <summary>
    /// Field name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Field type
    /// </summary>
    public MetadataType Type { get; set; }

    /// <summary>
    /// Is required field?
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
    /// Sort order
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Option items (for select, multiselect and checkbox type fields)
    /// </summary>
    public List<MetadataSchemaFieldOptionDto>? OptionItems { get; set; }
}