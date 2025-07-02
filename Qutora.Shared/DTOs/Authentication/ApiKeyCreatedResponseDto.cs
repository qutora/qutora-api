namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// Response returned after API Key creation operation
/// </summary>
public class ApiKeyCreatedResponseDto
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
    /// API Key secret part
    /// </summary>
    public string Secret { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Expiration date (null if permanent)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Permission level (ReadOnly, ReadWrite, FullAccess)
    /// </summary>
    public string Permission { get; set; }
}