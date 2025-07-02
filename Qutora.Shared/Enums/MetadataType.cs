namespace Qutora.Shared.Enums;

/// <summary>
/// Defines data types for metadata values
/// </summary>
public enum MetadataType
{
    /// <summary>
    /// Text, alphanumeric value
    /// </summary>
    Text = 0,

    /// <summary>
    /// Numeric value, integer or decimal
    /// </summary>
    Number = 1,

    /// <summary>
    /// Date and time value
    /// </summary>
    DateTime = 2,

    /// <summary>
    /// Boolean value (true/false)
    /// </summary>
    Boolean = 3,

    /// <summary>
    /// Single value from selection list
    /// </summary>
    Select = 4,

    /// <summary>
    /// Multiple values from selection list
    /// </summary>
    MultiSelect = 5,

    /// <summary>
    /// Reference to another file/resource
    /// </summary>
    Reference = 6
}