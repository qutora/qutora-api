namespace Qutora.Shared.DTOs.Dashboard
{
    /// <summary>
    /// Recent document data transfer object
    /// </summary>
    public class RecentDocumentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string FileType { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
    }
} 