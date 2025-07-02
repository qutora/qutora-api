namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// DTO that carries API Key information
/// </summary>
public class ApiKeyDto
{
    /// <summary>
    /// API Key ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// API Key name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// API Key public part
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Expiration date (null if permanent)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Last used date
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Whether the API Key is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Permission level (ReadOnly, ReadWrite, FullAccess)
    /// </summary>
    public string Permission { get; set; }

    /// <summary>
    /// Number of storage providers with access permission
    /// </summary>
    public int ProviderCount { get; set; }

    /// <summary>
    /// Storage provider IDs with access permission
    /// </summary>
    public List<Guid> AllowedProviderIds { get; set; } = new();
}