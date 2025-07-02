namespace Qutora.Application.Models.ApiKeys;

public class ApiKeyResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
    public string Permission { get; set; } = string.Empty;
    public int ProviderCount { get; set; }
}