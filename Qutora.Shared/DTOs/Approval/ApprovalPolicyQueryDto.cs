using Qutora.Shared.DTOs.Common;
using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs.Approval;

public class ApprovalPolicyQueryDto : PageRequestDto
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public ApprovalType? ApprovalType { get; set; }
}