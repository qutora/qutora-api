namespace Qutora.Shared.DTOs.Approval;

public class UpdateApprovalSettingsDto
{
    public bool? IsGlobalApprovalEnabled { get; set; }
    public string? GlobalApprovalReason { get; set; }

    public int? DefaultExpirationDays { get; set; }
    public int? DefaultRequiredApprovals { get; set; }

    public bool? ForceApprovalForAll { get; set; }
    public bool? ForceApprovalForLargeFiles { get; set; }
    public long? LargeFileSizeThresholdBytes { get; set; }

    public bool? EnableEmailNotifications { get; set; }
}