using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Approval;
using Qutora.Shared.Enums;

namespace Qutora.Application.Interfaces;

public interface IApprovalService
{
    Task<ShareApprovalRequestDto> CreateApprovalRequestAsync(
        Guid documentShareId,
        Guid policyId,
        string? requestReason = null,
        CancellationToken cancellationToken = default);

    Task<ApprovalResultDto> ProcessApprovalAsync(
        Guid approvalRequestId,
        ApprovalAction decision,
        string? comment = null,
        string? approverUserId = null,
        CancellationToken cancellationToken = default);

    Task<PagedDto<ShareApprovalRequestDto>> GetPendingApprovalsAsync(
        ApprovalRequestQueryDto query,
        CancellationToken cancellationToken = default);

    Task<PagedDto<ShareApprovalRequestDto>> GetMyApprovalRequestsAsync(
        ApprovalRequestQueryDto query,
        CancellationToken cancellationToken = default);

    Task<ShareApprovalRequestDto?> GetRequestByIdAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default);

    Task<List<ApprovalHistoryDto>> GetApprovalHistoryAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default);

    Task<ApprovalStatisticsDto> GetStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<bool> CanUserApproveRequestAsync(
        Guid approvalRequestId,
        string userId,
        CancellationToken cancellationToken = default);

    Task ProcessExpiredRequestsAsync(CancellationToken cancellationToken = default);
}