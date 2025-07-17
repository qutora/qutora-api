using Microsoft.AspNetCore.Http;

namespace Qutora.Shared.DTOs.DocumentOrchestration;

/// <summary>
/// Document create request
/// </summary>
public class DocumentCreateRequest
{
    public IFormFile File { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? BucketId { get; set; }
    public string? MetadataJson { get; set; }
    public string? MetadataSchemaId { get; set; }
    public bool CreateShare { get; set; }
    public int? ExpiresAfterDays { get; set; }
    public int? MaxViewCount { get; set; }
    public string? Password { get; set; }
    public string? WatermarkText { get; set; }
    public bool AllowDownload { get; set; } = true;
    public bool AllowPrint { get; set; } = true;
    public string? CustomMessage { get; set; }
    public bool NotifyOnAccess { get; set; }
    public string? NotificationEmails { get; set; }
    public string UserId { get; set; } = string.Empty;
    /// <summary>
    /// Is this a direct share (direct file access)?
    /// </summary>
    public bool IsDirectShare { get; set; }
}