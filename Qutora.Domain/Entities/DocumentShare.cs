using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;
using Qutora.Shared.Enums;

namespace Qutora.Domain.Entities;

public class DocumentShare : BaseEntity
{
    public Guid DocumentId { get; set; }
    public virtual Document Document { get; set; }

    [Required] [StringLength(12)] public string ShareCode { get; set; }

    
    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public int ViewCount { get; set; } = 0;

    /// <summary>
    /// Is password protection enabled?
    /// </summary>
    public bool IsPasswordProtected { get; set; } = false;

    /// <summary>
    /// Share password (hashed)
    /// </summary>
    [StringLength(500)]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Is download allowed?
    /// </summary>
    public bool AllowDownload { get; set; } = true;

    /// <summary>
    /// Is printing allowed?
    /// </summary>
    public bool AllowPrint { get; set; } = true;

    /// <summary>
    /// Maximum view count (null = unlimited)
    /// </summary>
    public int? MaxViewCount { get; set; }

    /// <summary>
    /// Watermark text
    /// </summary>
    [StringLength(200)]
    public string? WatermarkText { get; set; }

    /// <summary>
    /// Is watermark enabled?
    /// </summary>
    public bool ShowWatermark { get; set; } = false;

    /// <summary>
    /// Custom message to display with the share
    /// </summary>
    [StringLength(1000)]
    public string? CustomMessage { get; set; }

    /// <summary>
    /// Are email notifications enabled?
    /// </summary>
    public bool NotifyOnAccess { get; set; } = false;

    /// <summary>
    /// Email addresses to send notifications to (JSON array)
    /// </summary>
    [StringLength(2000)]
    public string? NotificationEmails { get; set; }

    /// <summary>
    /// Has the maximum view count been reached?
    /// </summary>
    public bool IsViewLimitReached => MaxViewCount.HasValue && ViewCount >= MaxViewCount.Value;


    /// <summary>
    /// Which API Key was used to create this share?
    /// </summary>
    public Guid? CreatedViaApiKeyId { get; set; }

    /// <summary>
    /// Does this share require approval?
    /// </summary>
    public bool RequiresApproval { get; set; } = false;

    /// <summary>
    /// Approval status
    /// </summary>
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.NotRequired;

    /// <summary>
    /// Is this a direct access share?
    /// Direct shares are automatically created for documents in buckets/categories with AllowDirectAccess = true
    /// </summary>
    public bool IsDirectShare { get; set; } = false;

    /// <summary>
    /// Unique token for approval action (one-time use)
    /// </summary>
    [StringLength(50)]
    public string? ApprovalToken { get; set; }

    /// <summary>
    /// Unique token for rejection action (one-time use)
    /// </summary>
    [StringLength(50)]
    public string? RejectionToken { get; set; }

    /// <summary>
    /// Has approval token been used?
    /// </summary>
    public bool IsApprovalTokenUsed { get; set; } = false;

    /// <summary>
    /// Has rejection token been used?
    /// </summary>
    public bool IsRejectionTokenUsed { get; set; } = false;

    public virtual ApiKey? CreatedViaApiKey { get; set; }
    public virtual ICollection<DocumentShareView> Views { get; set; } = new List<DocumentShareView>();
    public virtual ICollection<ShareApprovalRequest> ApprovalRequests { get; set; } = new List<ShareApprovalRequest>();
}