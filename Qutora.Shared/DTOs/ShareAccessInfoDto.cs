namespace Qutora.Shared.DTOs;

public class ShareAccessInfoDto
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public Guid ShareId { get; set; }

    public bool RequiresPassword { get; set; }
    public bool AllowDownload { get; set; } = true;
    public bool AllowPrint { get; set; } = true;
    public string? WatermarkText { get; set; }
    public bool ShowWatermark { get; set; }
    public string? CustomMessage { get; set; }
    public bool IsViewLimitReached { get; set; }
    public int? RemainingViews { get; set; }
}