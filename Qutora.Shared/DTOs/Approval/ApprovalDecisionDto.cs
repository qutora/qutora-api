using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs.Approval;

public class ApprovalDecisionDto
{
    public Guid Id { get; set; }
    public Guid ShareApprovalRequestId { get; set; }
    public string ApproverUserId { get; set; } = string.Empty;
    public string ApproverUserName { get; set; } = string.Empty;
    public string ApproverName { get; set; } = string.Empty;
    public ApprovalAction Decision { get; set; }
    public string? Comment { get; set; }
    public DateTime ApprovedAt { get; set; }
}