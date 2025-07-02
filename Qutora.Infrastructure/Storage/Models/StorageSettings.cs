namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// Storage provider settings.
/// </summary>
public class StorageSettings
{
    /// <summary>
    /// Provider unique identifier
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider config JSON data
    /// </summary>
    public string ConfigJson { get; set; } = "{}";

    /// <summary>
    /// Provider type
    /// </summary>
    public string ProviderType { get; set; } = string.Empty;
}
