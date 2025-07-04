namespace Qutora.Infrastructure.Interfaces;

/// <summary>
/// Service interface for managing blacklisted tokens
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Adds a token to the blacklist
    /// </summary>
    /// <param name="jti">Token's unique identifier (JWT ID)</param>
    /// <param name="expiryTime">Token's expiry time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation success</returns>
    Task<bool> AddToBlacklistAsync(string jti, DateTime expiryTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a token is blacklisted
    /// </summary>
    /// <param name="jti">Token's unique identifier (JWT ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if token is blacklisted, otherwise false</returns>
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired blacklist entries
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}