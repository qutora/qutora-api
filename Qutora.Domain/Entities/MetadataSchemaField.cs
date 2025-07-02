using System.ComponentModel.DataAnnotations;
using Qutora.Shared.Enums;

namespace Qutora.Domain.Entities;

/// <summary>
/// Metadata schema field definition
/// </summary>
public class MetadataSchemaField
{
    /// <summary>
    /// Field name
    /// </summary>
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Field display name
    /// </summary>
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Field description
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Field type (text, number, date, etc.)
    /// </summary>
    public MetadataType Type { get; set; } = MetadataType.Text;

    /// <summary>
    /// Is field required
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Default value
    /// </summary>
    [MaxLength(500)]
    public string DefaultValue { get; set; } = string.Empty;

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
    /// Validation regex (for text fields)
    /// </summary>
    [MaxLength(500)]
    public string ValidationRegex { get; set; } = string.Empty;

    /// <summary>
    /// Display order
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// Advanced option list (contains label, value and default information)
    /// </summary>
    public List<MetadataSchemaFieldOption> OptionItems { get; set; } = new();
}