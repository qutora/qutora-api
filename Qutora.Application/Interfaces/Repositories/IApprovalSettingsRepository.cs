using Qutora.Domain.Entities;

namespace Qutora.Application.Interfaces.Repositories;

public interface IApprovalSettingsRepository : IRepository<ApprovalSettings>
{
    Task<ApprovalSettings?> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<ApprovalSettings> GetOrCreateDefaultAsync(CancellationToken cancellationToken = default);
}