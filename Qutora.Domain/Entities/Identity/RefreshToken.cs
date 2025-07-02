namespace Qutora.Domain.Entities.Identity;

/// <summary>
/// Stores refresh token information
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Refresh token ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Refresh token value
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token owner user ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Token owner user
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Associated JWT token's unique identifier (JWT ID)
    /// </summary>
    public string? JwtId { get; set; }

    /// <summary>
    /// Expiry date
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Is it used
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Is it revoked
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// New token when renewed
    /// </summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// Creation time
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creating IP address
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// Revocation time
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Revoking IP address
    /// </summary>
    public string? RevokedByIp { get; set; }
}