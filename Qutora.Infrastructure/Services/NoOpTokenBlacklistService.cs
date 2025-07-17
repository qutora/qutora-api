
using Qutora.Application.Interfaces;

namespace Qutora.Infrastructure.Services;

/// <summary>
/// No-operation token blacklist implementation (no operations performed)
/// </summary>
public class NoOpTokenBlacklistService : ITokenBlacklistService
{
    /// <inheritdoc/>
    public Task<bool> AddToBlacklistAsync(string jti, DateTime expiryTime,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
