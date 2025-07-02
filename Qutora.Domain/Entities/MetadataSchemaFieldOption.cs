namespace Qutora.Domain.Entities;

/// <summary>
/// Metadata schema field option
/// </summary>
public class MetadataSchemaFieldOption
{
    /// <summary>
    /// Display label
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Actual value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Is this the default value
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Option order
    /// </summary>
    public int Order { get; set; } = 0;
}