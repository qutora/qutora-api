using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.Enums;

namespace Qutora.Domain.Entities;

public class ApprovalHistory : BaseEntity
{
    [Required]
    public Guid ShareApprovalRequestId { get; set; }
    public virtual ShareApprovalRequest? ShareApprovalRequest { get; set; }

    public ApprovalAction Action { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public string ActionByUserId { get; set; } = string.Empty;
    public virtual ApplicationUser? ActionByUser { get; set; }

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;

    public string? Metadata { get; set; }
} 