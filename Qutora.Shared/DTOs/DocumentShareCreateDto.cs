namespace Qutora.Shared.DTOs;

public class DocumentShareCreateDto
{
    public Guid DocumentId { get; set; }
    public int? ExpiresAfterDays { get; set; }


    /// <summary>
    /// Share password (plain text, will be hashed)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Download permission
    /// </summary>
    public bool AllowDownload { get; set; } = true;

    /// <summary>
    /// Print permission
    /// </summary>
    public bool AllowPrint { get; set; } = true;

    /// <summary>
    /// Maximum view count
    /// </summary>
    public int? MaxViewCount { get; set; }

    /// <summary>
    /// Watermark text
    /// </summary>
    public string? WatermarkText { get; set; }

    /// <summary>
    /// Should watermark be shown?
    /// </summary>
    public bool ShowWatermark { get; set; } = false;

    /// <summary>
    /// Custom message
    /// </summary>
    public string? CustomMessage { get; set; }

    /// <summary>
    /// Are email notifications active?
    /// </summary>
    public bool NotifyOnAccess { get; set; } = false;

    /// <summary>
    /// Notification email addresses
    /// </summary>
    public List<string>? NotificationEmails { get; set; }

    /// <summary>
    /// Is this a direct access share?
    /// </summary>
    public bool IsDirectShare { get; set; } = false;
}