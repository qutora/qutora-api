using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Domain.Entities.Identity;

namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// RefreshToken repository implementation following Clean Architecture standards
/// </summary>
public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context, ILogger<RefreshTokenRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<RefreshToken?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RefreshToken?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<RefreshToken, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public void UpdateRange(IEnumerable<RefreshToken> entities)
    {
        _dbSet.UpdateRange(entities);
    }
} 