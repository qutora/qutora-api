using Qutora.Shared.Enums;

namespace Qutora.Shared.Models;

/// <summary>
/// Cached version of API Key entity optimized for fast lookup
/// </summary>
public class CachedApiKey
{
    /// <summary>
    /// API Key unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Public API key string
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Hashed secret for validation
    /// </summary>
    public string SecretHash { get; set; } = string.Empty;

    /// <summary>
    /// Owner user ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Friendly name for the API key
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether the API key is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Expiration date (null = never expires)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Last usage timestamp
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Permission level for this API key
    /// </summary>
    public ApiKeyPermission Permissions { get; set; }

    /// <summary>
    /// List of provider IDs this key can access (empty = all accessible providers)
    /// </summary>
    public List<Guid> AllowedProviderIds { get; set; } = new();

    /// <summary>
    /// When this entry was cached
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Checks if the API key is valid for use
    /// </summary>
    public bool IsValidForUse => IsActive && (!ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow);

    /// <summary>
    /// Checks if this API key can access the specified provider
    /// </summary>
    public bool CanAccessProvider(Guid providerId) => 
        !AllowedProviderIds.Any() || AllowedProviderIds.Contains(providerId);
} 