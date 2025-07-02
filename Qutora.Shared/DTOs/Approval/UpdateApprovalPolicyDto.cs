namespace Qutora.Shared.DTOs.Approval;

public class UpdateApprovalPolicyDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public int? Priority { get; set; }
    public List<Guid>? CategoryFilters { get; set; }
    public List<Guid>? ProviderFilters { get; set; }
    public List<string>? UserFilters { get; set; }
    public List<Guid>? ApiKeyFilters { get; set; }
    public int? FileSizeLimitMB { get; set; }
    public List<string>? FileTypeFilters { get; set; }
    public bool? RequireApproval { get; set; }
    public int? ApprovalTimeoutHours { get; set; }
    public int? RequiredApprovalCount { get; set; }
}