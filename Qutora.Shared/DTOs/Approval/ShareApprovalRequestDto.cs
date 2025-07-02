using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs.Approval;

public class ShareApprovalRequestDto
{
    public Guid Id { get; set; }
    public Guid DocumentShareId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ShareCode { get; set; } = string.Empty;
    public Guid ApprovalPolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public ApprovalStatus Status { get; set; }
    public string? RequestReason { get; set; }
    public string? FinalComment { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public string RequestedByUserName { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public int RequiredApprovalCount { get; set; }
    public int CurrentApprovalCount { get; set; }
    public List<string>? AssignedApprovers { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public ApprovalPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsPending => Status == ApprovalStatus.Pending;
    public bool IsApproved => Status == ApprovalStatus.Approved;
    public bool IsRejected => Status == ApprovalStatus.Rejected;
}