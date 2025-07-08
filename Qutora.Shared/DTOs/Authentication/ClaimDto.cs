namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// Data transfer object for claims
/// </summary>
public class ClaimDto
{
    /// <summary>
    /// Claim type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Claim value
    /// </summary>
    public string Value { get; set; } = string.Empty;
}