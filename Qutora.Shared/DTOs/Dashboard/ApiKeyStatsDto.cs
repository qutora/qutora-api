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

    /// <summary>
    /// Top API Key usage data transfer object
    /// </summary>
    public class TopApiKeyUsageDto
    {
        public string KeyId { get; set; } = string.Empty;
        public string KeyName { get; set; } = string.Empty;
        public int CallCount { get; set; }
        public DateTime LastUsed { get; set; }
    }
} 