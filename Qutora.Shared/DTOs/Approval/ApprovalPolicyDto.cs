namespace Qutora.Shared.DTOs.Approval;

public class ApprovalPolicyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public List<Guid>? CategoryFilters { get; set; }
    public List<Guid>? ProviderFilters { get; set; }
    public List<string>? UserFilters { get; set; }
    public List<Guid>? ApiKeyFilters { get; set; }
    public int? FileSizeLimitMB { get; set; }
    public List<string>? FileTypeFilters { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    public bool RequireApproval { get; set; }
    public int ApprovalTimeoutHours { get; set; }
    public int RequiredApprovalCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}