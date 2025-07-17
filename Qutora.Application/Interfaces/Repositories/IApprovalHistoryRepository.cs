using Qutora.Domain.Entities;

namespace Qutora.Application.Interfaces.Repositories;

public interface IApprovalHistoryRepository : IRepository<ApprovalHistory>
{
    Task<IEnumerable<ApprovalHistory>> GetByApprovalRequestIdAsync(Guid approvalRequestId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ApprovalHistory>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<IEnumerable<ApprovalHistory>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default);
}