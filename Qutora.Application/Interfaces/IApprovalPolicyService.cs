using Qutora.Domain.Entities;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Approval;

namespace Qutora.Application.Interfaces;

public interface IApprovalPolicyService
{
    Task<ApprovalPolicyDto> CreateAsync(CreateApprovalPolicyDto dto, CancellationToken cancellationToken = default);

    Task<ApprovalPolicyDto?> UpdateAsync(Guid id, UpdateApprovalPolicyDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApprovalPolicyDto?> TogglePolicyStatusAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApprovalPolicyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDto<ApprovalPolicyDto>> GetPagedAsync(ApprovalPolicyQueryDto query,
        CancellationToken cancellationToken = default);

    Task<ApprovalPolicy?> GetApplicablePolicyAsync(DocumentShare documentShare,
        CancellationToken cancellationToken = default);

    Task<bool> EvaluatePolicyAsync(ApprovalPolicy policy, Document document, ApplicationUser user, DocumentShare share,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetAssignedApproversAsync(ApprovalPolicy policy, DocumentShare share,
        CancellationToken cancellationToken = default);

    Task<ApprovalPolicy?> GetDefaultPolicyAsync(CancellationToken cancellationToken = default);

    Task<List<ApprovalPolicyDto>> GetApplicablePoliciesAsync(DocumentShare documentShare,
        CancellationToken cancellationToken = default);

    Task<bool> EvaluateApprovalRequirementAsync(DocumentShare documentShare,
        CancellationToken cancellationToken = default);
}