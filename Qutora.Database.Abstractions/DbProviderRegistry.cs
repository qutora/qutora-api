using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Qutora.Database.Abstractions;

/// <summary>
/// Veritabanı sağlayıcılarını yönetmek için registry implementasyonu
/// </summary>
public class DbProviderRegistry(ILogger<DbProviderRegistry>? logger = null) : IDbProviderRegistry
{
    private readonly ConcurrentDictionary<string, IDbProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Bir veritabanı sağlayıcısını kaydeder
    /// </summary>
    public void RegisterProvider(IDbProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        if (string.IsNullOrEmpty(provider.ProviderName))
            throw new ArgumentException("Provider name cannot be empty", nameof(provider));

        _providers[provider.ProviderName] = provider;
        logger?.LogInformation("Database provider registered: {ProviderName}", provider.ProviderName);
    }

    /// <summary>
    /// İsim ile sağlayıcı alır
    /// </summary>
    public IDbProvider? GetProvider(string providerName)
    {
        if (string.IsNullOrEmpty(providerName))
            return null;

        if (_providers.TryGetValue(providerName, out var provider))
            return provider;

        logger?.LogWarning("Database provider not found: {ProviderName}", providerName);
        return null;
    }

    /// <summary>
    /// Kayıtlı tüm sağlayıcı adlarını döndürür
    /// </summary>
    public IEnumerable<string> GetAvailableProviders()
    {
        return _providers.Keys;
    }

    /// <summary>
    /// EF Core provider adından sağlayıcı adını çıkarır
    /// </summary>
    public string ExtractProviderName(string? providerName)
    {
        if (string.IsNullOrEmpty(providerName))
            return "SqlServer";

        if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            return "SqlServer";

        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            return "PostgreSQL";

        if (providerName.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            return "MySQL";

        return "SqlServer"; 
    }
}