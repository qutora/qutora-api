namespace Qutora.Shared.DTOs.Dashboard
{
    /// <summary>
    /// User document statistics data transfer object
    /// </summary>
    public class UserDocumentStatsDto
    {
        public int MyDocuments { get; set; }
        public int MyMonthlyUploads { get; set; }
        public decimal MyStorageUsage { get; set; }
        public int MyTodayUploads { get; set; }
        public int MyWeeklyUploads { get; set; }
        public decimal MyAvgFileSize { get; set; }
        public string MyMostUsedFileType { get; set; } = string.Empty;
        public decimal StorageUsagePercentage { get; set; }
    }
} 