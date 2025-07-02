using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.Enums;

namespace Qutora.Domain.Entities;

public class ShareApprovalRequest : BaseEntity
{
    [Required]
    public Guid DocumentShareId { get; set; }
    public virtual DocumentShare? DocumentShare { get; set; }

    [Required]
    public Guid ApprovalPolicyId { get; set; }
    public virtual ApprovalPolicy? ApprovalPolicy { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    [StringLength(1000)]
    public string? RequestReason { get; set; }

    [StringLength(1000)]
    public string? FinalComment { get; set; }

    [Required]
    public string RequestedByUserId { get; set; } = string.Empty;
    public virtual ApplicationUser? RequestedByUser { get; set; }

    public int RequiredApprovalCount { get; set; } = 1;

    public int CurrentApprovalCount { get; set; } = 0;

    public string? AssignedApprovers { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public ApprovalPriority Priority { get; set; } = ApprovalPriority.Normal;

    public virtual ICollection<ApprovalDecision> ApprovalDecisions { get; set; } = new List<ApprovalDecision>();

    public virtual ICollection<ApprovalHistory> ApprovalHistory { get; set; } = new List<ApprovalHistory>();
} 