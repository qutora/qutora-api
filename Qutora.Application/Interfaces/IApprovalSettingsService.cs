using Qutora.Domain.Entities;
using Qutora.Shared.DTOs.Approval;

namespace Qutora.Application.Interfaces;

public interface IApprovalSettingsService
{
    Task<ApprovalSettingsDto> GetCurrentSettingsAsync(CancellationToken cancellationToken = default);

    Task EnableGlobalApprovalAsync(
        string reason,
        string? adminUserId = null,
        CancellationToken cancellationToken = default);

    Task DisableGlobalApprovalAsync(
        string? adminUserId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsGlobalApprovalEnabledAsync(CancellationToken cancellationToken = default);

    Task<ApprovalSettingsDto> UpdateSettingsAsync(UpdateApprovalSettingsDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> RequiresApprovalAsync(DocumentShare documentShare, CancellationToken cancellationToken = default);

    Task<ApprovalPolicy> EnsureDefaultPolicyExistsAsync(CancellationToken cancellationToken = default);

    Task<ApprovalPolicy> EnsureGlobalSystemPolicyExistsAsync(CancellationToken cancellationToken = default);
}