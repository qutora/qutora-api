using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs.Approval;

public class ApprovalResultDto
{
    public bool RequiresApproval { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public ApprovalStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}