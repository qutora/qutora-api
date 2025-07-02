namespace Qutora.Application.Identity;

/// <summary>
/// JWT settings
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// JWT signing key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token validity duration (minutes)
    /// </summary>
    public int DurationInMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token validity duration (days)
    /// </summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;

    /// <summary>
    /// Access token clock skew tolerance (minutes)
    /// </summary>
    public int AccessTokenClockSkew { get; set; } = 0;

    /// <summary>
    /// Require HTTPS metadata
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Save token
    /// </summary>
    public bool SaveToken { get; set; } = true;

    /// <summary>
    /// Validate issuer signing key
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Validate issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Validate audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Validate lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Determines whether the token response should be simplified.
    /// If true, only token, refreshToken, expires and userId are returned.
    /// If false, all user information is returned.
    /// </summary>
    public bool MinimalResponse { get; set; } = false;

    /// <summary>
    /// Token blacklist mechanism enabled
    /// </summary>
    public bool TokenBlacklistEnabled { get; set; } = true;
}
