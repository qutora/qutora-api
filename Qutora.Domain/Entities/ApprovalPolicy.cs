using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;

namespace Qutora.Domain.Entities;

public class ApprovalPolicy : BaseEntity
{
    [Required] [StringLength(200)] public string Name { get; set; } = string.Empty;

    [StringLength(1000)] public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int Priority { get; set; } = 1;

    public bool RequireApproval { get; set; } = true;

    public int ApprovalTimeoutHours { get; set; } = 72;

    public int RequiredApprovalCount { get; set; } = 1;

    public string? CategoryFilters { get; set; }
    public string? ProviderFilters { get; set; }
    public string? UserFilters { get; set; }
    public string? ApiKeyFilters { get; set; }
    public int? FileSizeLimitMB { get; set; }
    public string? FileTypeFilters { get; set; }

    public virtual ICollection<ShareApprovalRequest> ApprovalRequests { get; set; } = new List<ShareApprovalRequest>();
}