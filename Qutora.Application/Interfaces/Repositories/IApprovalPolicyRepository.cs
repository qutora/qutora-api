using Qutora.Domain.Entities;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Approval;

namespace Qutora.Application.Interfaces.Repositories;

public interface IApprovalPolicyRepository : IRepository<ApprovalPolicy>
{
    Task<IEnumerable<ApprovalPolicy>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<ApprovalPolicy?> GetDefaultForcedPolicyAsync(CancellationToken cancellationToken = default);

    Task<ApprovalPolicy?> GetGlobalSystemPolicyAsync(CancellationToken cancellationToken = default);

    Task<bool> IsGlobalSystemPolicyAsync(Guid policyId, CancellationToken cancellationToken = default);

    Task<IEnumerable<ApprovalPolicy>> GetByCreatedByUserAsync(string userId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ApprovalPolicy>> GetByFiltersAsync(
        bool? isActive = null,
        bool? requireApproval = null,
        CancellationToken cancellationToken = default);

    Task<PagedDto<ApprovalPolicy>> GetPagedAsync(ApprovalPolicyQueryDto query,
        CancellationToken cancellationToken = default);
}