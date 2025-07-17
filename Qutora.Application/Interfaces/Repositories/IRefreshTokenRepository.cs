using Qutora.Domain.Entities.Identity;
using System.Linq.Expressions;

namespace Qutora.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
    Task<RefreshToken?> FirstOrDefaultAsync(Expression<Func<RefreshToken, bool>> predicate, CancellationToken cancellationToken = default);
    void UpdateRange(IEnumerable<RefreshToken> entities);
} 