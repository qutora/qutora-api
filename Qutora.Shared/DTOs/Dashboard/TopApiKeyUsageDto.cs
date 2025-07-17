namespace Qutora.Shared.DTOs.Dashboard;

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