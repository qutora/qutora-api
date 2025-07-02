namespace Qutora.Shared.DTOs.Approval;

public class ApprovalSettingsDto
{
    public Guid Id { get; set; }

    public bool IsGlobalApprovalEnabled { get; set; }
    public DateTime? GlobalApprovalEnabledAt { get; set; }
    public string? GlobalApprovalEnabledByUserId { get; set; }
    public string? GlobalApprovalEnabledByUserName { get; set; }
    public string? GlobalApprovalReason { get; set; }

    public int DefaultExpirationDays { get; set; }
    public int DefaultRequiredApprovals { get; set; }

    public bool ForceApprovalForAll { get; set; }
    public bool ForceApprovalForLargeFiles { get; set; }
    public long LargeFileSizeThresholdBytes { get; set; }

    public bool EnableEmailNotifications { get; set; }

    public string UpdatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}