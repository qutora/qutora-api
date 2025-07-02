using Qutora.Domain.Entities;

namespace Qutora.Infrastructure.Interfaces.Repositories;

public interface IApprovalDecisionRepository : IRepository<ApprovalDecision>
{
    Task<IEnumerable<ApprovalDecision>> GetByApprovalRequestIdAsync(Guid approvalRequestId,
        CancellationToken cancellationToken = default);

    Task<ApprovalDecision?> GetByApprovalRequestAndUserAsync(Guid approvalRequestId, string userId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ApprovalDecision>> GetByApproverUserAsync(string userId,
        CancellationToken cancellationToken = default);

    Task<bool> HasUserAlreadyDecidedAsync(Guid approvalRequestId, string userId,
        CancellationToken cancellationToken = default);
}