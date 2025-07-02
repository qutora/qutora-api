namespace Qutora.Shared.DTOs.Approval;

/// <summary>
/// Request model for enabling global approval
/// </summary>
public class EnableGlobalApprovalRequestDto
{
    public required string Reason { get; set; }
} 