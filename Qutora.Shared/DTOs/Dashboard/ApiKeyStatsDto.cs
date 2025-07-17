namespace Qutora.Shared.DTOs.Dashboard
{
    /// <summary>
    /// API Key statistics data transfer object
    /// </summary>
    public class ApiKeyStatsDto
    {
        public int TotalApiKeys { get; set; }
        public int ActiveApiKeys { get; set; }
        public int DailyApiCalls { get; set; }
        public int WeeklyApiCalls { get; set; }
        public int MonthlyApiCalls { get; set; }
        public decimal ApiErrorRate { get; set; }
        public int ExpiredApiKeys { get; set; }
        public int ExpiringApiKeys { get; set; }
        public List<TopApiKeyUsageDto> TopUsedKeys { get; set; } = new();
    }
} 