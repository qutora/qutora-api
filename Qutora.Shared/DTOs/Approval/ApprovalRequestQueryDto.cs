using Qutora.Shared.DTOs.Common;
using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs.Approval;

public class ApprovalRequestQueryDto : PageRequestDto
{
    public ApprovalStatus? Status { get; set; }
    public string? RequesterUserId { get; set; }
    public DateTime? RequestedAfter { get; set; }
    public DateTime? RequestedBefore { get; set; }
}