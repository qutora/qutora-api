namespace Qutora.Shared.DTOs.Dashboard
{
    /// <summary>
    /// Document statistics data transfer object
    /// </summary>
    public class DocumentStatsDto
    {
        public int TotalDocuments { get; set; }
        public int MonthlyUploads { get; set; }
        public decimal StorageUsage { get; set; }
        public int TodayUploads { get; set; }
        public int WeeklyUploads { get; set; }
        public decimal AvgFileSize { get; set; }
        public string MostUsedFileType { get; set; } = string.Empty;
    }
} 