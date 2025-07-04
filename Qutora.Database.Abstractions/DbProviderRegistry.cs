using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Qutora.Database.Abstractions;

/// <summary>
/// Registry implementation for managing database providers
/// </summary>
public class DbProviderRegistry(ILogger<DbProviderRegistry>? logger = null) : IDbProviderRegistry
{
    private readonly ConcurrentDictionary<string, IDbProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a database provider
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
    /// Gets provider by name
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
    /// Returns all registered provider names
    /// </summary>
    public IEnumerable<string> GetAvailableProviders()
    {
        return _providers.Keys;
    }

    /// <summary>
    /// Extracts provider name from EF Core provider name
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