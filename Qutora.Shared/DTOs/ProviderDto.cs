namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO containing storage provider information
/// </summary>
public class ProviderDto
{
    /// <summary>
    /// Provider ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Provider name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Provider display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Is default provider
    /// </summary>
    public bool IsDefault { get; set; }
}