using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs;

public class DocumentShareDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string ShareCode { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int ViewCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    public string ShareUrl { get; set; } = string.Empty;

    public bool IsPasswordProtected { get; set; }
    public bool AllowDownload { get; set; }
    public bool AllowPrint { get; set; }
    public int? MaxViewCount { get; set; }
    public string? WatermarkText { get; set; }
    public bool ShowWatermark { get; set; }
    public string? CustomMessage { get; set; }
    public bool NotifyOnAccess { get; set; }
    public List<string>? NotificationEmails { get; set; }
    public bool IsViewLimitReached => MaxViewCount.HasValue && ViewCount >= MaxViewCount.Value;

    public bool RequiresApproval { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    
    /// <summary>
    /// Is this a direct access share?
    /// </summary>
    public bool IsDirectShare { get; set; }
    
    /// <summary>
    /// File extension for direct access
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;
}