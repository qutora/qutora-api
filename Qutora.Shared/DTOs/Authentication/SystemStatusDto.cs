namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// DTO containing system status information
/// </summary>
public class SystemStatusDto
{
    /// <summary>
    /// Is the system initialized?
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// System version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Response timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}