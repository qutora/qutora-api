namespace Qutora.Domain.Entities;

/// <summary>
/// Entity that holds general system status information
/// </summary>
public class SystemSettings
{
    /// <summary>
    /// Unique identifier (primary key)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Indicates whether the initial system setup has been completed
    /// </summary>
    public bool IsInitialized { get; set; } = false;

    /// <summary>
    /// Date when initial setup was completed
    /// </summary>
    public DateTime? InitializedAt { get; set; }

    /// <summary>
    /// User ID who completed the initial setup
    /// </summary>
    public string? InitializedByUserId { get; set; }

    /// <summary>
    /// Application/company name
    /// </summary>
    public string ApplicationName { get; set; } = "Qutora";

    /// <summary>
    /// System version
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last updated date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}