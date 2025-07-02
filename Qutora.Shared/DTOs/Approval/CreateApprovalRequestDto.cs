using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs.Approval;

public class CreateApprovalRequestDto
{
    [Required] public Guid DocumentShareId { get; set; }

    [StringLength(1000)] public string? RequestReason { get; set; }
}