using Qutora.Domain.Entities;

namespace Qutora.Infrastructure.Interfaces.Repositories;

public interface IApprovalSettingsRepository : IRepository<ApprovalSettings>
{
    Task<ApprovalSettings?> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<ApprovalSettings> GetOrCreateDefaultAsync(CancellationToken cancellationToken = default);
}