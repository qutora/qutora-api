using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.Enums;

namespace Qutora.Domain.Entities;

public class ApprovalDecision : BaseEntity
{
    [Required]
    public Guid ShareApprovalRequestId { get; set; }
    public virtual ShareApprovalRequest? ShareApprovalRequest { get; set; }

    [Required]
    public string ApproverUserId { get; set; } = string.Empty;
    public virtual ApplicationUser? ApproverUser { get; set; }

    public ApprovalAction Decision { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
} 