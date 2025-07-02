using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs.Approval;

public class ApprovalHistoryDto
{
    public Guid Id { get; set; }
    public Guid ShareApprovalRequestId { get; set; }
    public ApprovalAction Action { get; set; }
    public string ActionByUserId { get; set; } = string.Empty;
    public string? ActionByUserName { get; set; }
    public DateTime ActionDate { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? Comment { get; set; }
}