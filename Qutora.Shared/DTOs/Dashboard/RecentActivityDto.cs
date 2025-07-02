namespace Qutora.Shared.DTOs.Dashboard
{
    /// <summary>
    /// Recent activity data transfer object
    /// </summary>
    public class RecentActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string IconClass { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? EntityName { get; set; }
    }
} 