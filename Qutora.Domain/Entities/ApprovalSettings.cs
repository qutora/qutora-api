using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;
using Qutora.Domain.Entities.Identity;

namespace Qutora.Domain.Entities;

public class ApprovalSettings : BaseEntity
{
    public bool IsGlobalApprovalEnabled { get; set; } = false;
    public DateTime? GlobalApprovalEnabledAt { get; set; }
    public string? GlobalApprovalEnabledByUserId { get; set; }
    public virtual ApplicationUser? GlobalApprovalEnabledByUser { get; set; }
    
    [StringLength(500)]
    public string? GlobalApprovalReason { get; set; }

    public int DefaultExpirationDays { get; set; } = 7;
    public int DefaultRequiredApprovals { get; set; } = 1;

    public bool ForceApprovalForAll { get; set; } = false;
    public bool ForceApprovalForLargeFiles { get; set; } = true;
    public long LargeFileSizeThresholdBytes { get; set; } = 100 * 1024 * 1024;

    public bool EnableEmailNotifications { get; set; } = true;
} 