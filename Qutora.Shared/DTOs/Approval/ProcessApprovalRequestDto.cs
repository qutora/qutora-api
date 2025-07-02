using System.ComponentModel.DataAnnotations;
using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs.Approval;

/// <summary>
/// Request model for processing approval
/// </summary>
public class ProcessApprovalRequestDto
{
    [Required] public ApprovalAction Decision { get; set; }

    [StringLength(1000)] public string? Comment { get; set; }
}