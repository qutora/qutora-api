namespace Qutora.Shared.DTOs.Dashboard
{
    /// <summary>
    /// User API Key statistics data transfer object
    /// </summary>
    public class UserApiKeyStatsDto
    {
        public int MyApiKeys { get; set; }
        public int MyActiveApiKeys { get; set; }
        public int MyDailyApiCalls { get; set; }
        public int MyWeeklyApiCalls { get; set; }
        public int MyMonthlyApiCalls { get; set; }
        public decimal MyApiErrorRate { get; set; }
        public int MyExpiredApiKeys { get; set; }
        public int MyExpiringApiKeys { get; set; }
        public List<TopApiKeyUsageDto> MyTopUsedKeys { get; set; } = new();
    }
} 