namespace Qutora.Shared.DTOs;

/// <summary>
/// Data transfer object for metadata schema field option items
/// </summary>
public class MetadataSchemaFieldOptionDto
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
    /// Is default value
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Option order
    /// </summary>
    public int Order { get; set; }
}