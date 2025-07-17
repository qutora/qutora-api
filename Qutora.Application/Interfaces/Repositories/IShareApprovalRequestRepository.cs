using Qutora.Domain.Entities;
using Qutora.Shared.Enums;

namespace Qutora.Application.Interfaces.Repositories;

public interface IShareApprovalRequestRepository : IRepository<ShareApprovalRequest>
{
    Task<ShareApprovalRequest?> GetByDocumentShareIdAsync(Guid documentShareId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ShareApprovalRequest>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ShareApprovalRequest>> GetPendingRequestsForUserAsync(string userId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ShareApprovalRequest>> GetByStatusAsync(ApprovalStatus status,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ShareApprovalRequest>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ShareApprovalRequest>> GetExpiredRequestsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ShareApprovalRequest>> GetByRequestedByUserAsync(string userId,
        CancellationToken cancellationToken = default);
}