namespace Qutora.Shared.DTOs.Approval;

public class CreateApprovalPolicyDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 1;
    public int ApprovalTimeoutHours { get; set; } = 72;
    public int RequiredApprovalCount { get; set; } = 1;
    public List<Guid>? CategoryIds { get; set; }
    public List<Guid>? CategoryFilters { get; set; }
    public List<Guid>? ProviderFilters { get; set; }
    public List<string>? UserFilters { get; set; }
    public List<Guid>? ApiKeyFilters { get; set; }
    public int? FileSizeLimitMB { get; set; }
    public decimal? MinFileSizeMB { get; set; }
    public decimal? MaxFileSizeMB { get; set; }
    public string? FileExtensions { get; set; }
    public List<string>? FileTypeFilters { get; set; }
    public List<string>? RequiredApproverRoles { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public bool RequireApproval { get; set; } = true;
}