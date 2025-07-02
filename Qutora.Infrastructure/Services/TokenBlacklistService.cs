using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Qutora.Infrastructure.Interfaces;

namespace Qutora.Infrastructure.Services;

/// <summary>
/// Memory-based token blacklist implementation
/// </summary>
public class TokenBlacklistService(ILogger<TokenBlacklistService> logger) : ITokenBlacklistService
{
    private readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = new();
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private DateTime _lastCleanup = DateTime.UtcNow;

    /// <inheritdoc/>
    public Task<bool> AddToBlacklistAsync(string jti, DateTime expiryTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = _blacklistedTokens.TryAdd(jti, expiryTime);

            if (DateTime.UtcNow - _lastCleanup > _cleanupInterval) _ = CleanupExpiredTokensAsync(cancellationToken);

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding token to blacklist. JTI: {jti}", jti);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_blacklistedTokens.ContainsKey(jti));
    }

    /// <inheritdoc/>
    public Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredTokens = _blacklistedTokens
                .Where(token => token.Value <= now)
                .Select(token => token.Key)
                .ToList();

            foreach (var jti in expiredTokens) _blacklistedTokens.TryRemove(jti, out _);

            _lastCleanup = now;
            logger.LogInformation("Cleaned up {count} expired tokens", expiredTokens.Count);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while cleaning up expired tokens");
            return Task.CompletedTask;
        }
    }
}
