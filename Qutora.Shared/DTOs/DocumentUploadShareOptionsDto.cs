namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for share options during upload
/// </summary>
public class DocumentUploadShareOptionsDto
{
    /// <summary>
    /// Should a share be created?
    /// </summary>
    public bool CreateShare { get; set; }

    /// <summary>
    /// Validity period (in days)
    /// </summary>
    public int? ExpiresAfterDays { get; set; }

    /// <summary>
    /// Maximum view count
    /// </summary>
    public int? MaxViewCount { get; set; }

    /// <summary>
    /// Password protection
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Watermark text
    /// </summary>
    public string? WatermarkText { get; set; }

    /// <summary>
    /// Allow download
    /// </summary>
    public bool AllowDownload { get; set; } = true;

    /// <summary>
    /// Allow printing
    /// </summary>
    public bool AllowPrint { get; set; } = true;

    /// <summary>
    /// Custom message
    /// </summary>
    public string? CustomMessage { get; set; }

    /// <summary>
    /// Send email notification
    /// </summary>
    public bool NotifyOnAccess { get; set; }

    /// <summary>
    /// Notification emails
    /// </summary>
    public List<string>? NotificationEmails { get; set; }

    /// <summary>
    /// Is this a direct share (direct file access)?
    /// </summary>
    public bool IsDirectShare { get; set; }
}